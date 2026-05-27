package com.ipts.ambulance.ui.job

import android.util.Log
import androidx.lifecycle.ViewModel
import androidx.lifecycle.viewModelScope
import com.ipts.ambulance.data.api.CrewApiService
import com.ipts.ambulance.data.model.CrewActiveJobDto
import com.ipts.ambulance.data.model.UpdateLocationRequest
import com.ipts.ambulance.data.storage.TokenManager
import dagger.hilt.android.lifecycle.HiltViewModel
import kotlinx.coroutines.Job
import kotlinx.coroutines.delay
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.firstOrNull
import kotlinx.coroutines.isActive
import kotlinx.coroutines.launch
import javax.inject.Inject

data class JobUiState(
    val job:       CrewActiveJobDto? = null,
    val isLoading: Boolean           = false,
    val error:     String?            = null,
    val noJob:     Boolean            = false
)

@HiltViewModel
class JobViewModel @Inject constructor(
    private val api: CrewApiService,
    private val tokenManager: TokenManager
) : ViewModel() {

    private val _state = MutableStateFlow(JobUiState())
    val state: StateFlow<JobUiState> = _state

    init { loadJob() }

    fun loadJob() {
        viewModelScope.launch {
            _state.value = JobUiState(isLoading = true)
            val token = tokenManager.token.firstOrNull()
            if (token == null) {
                _state.value = JobUiState(error = "Not logged in.")
                return@launch
            }
            try {
                val response = api.getActiveJob("Bearer $token")
                when {
                    response.isSuccessful ->
                        _state.value = JobUiState(job = response.body())
                    response.code() == 404 ->
                        _state.value = JobUiState(noJob = true)
                    else ->
                        _state.value = JobUiState(
                            error = "Server error: ${response.code()}")
                }
            } catch (e: Exception) {
                _state.value = JobUiState(
                    error = "Cannot connect to server.")
            }
        }
    }

    // ── GPS location posting ──────────────────────
    private var locationJob: Job? = null

    fun startLocationUpdates(
        token: String,
        baseLat: Double,
        baseLng: Double
    ) {
        locationJob?.cancel()
        locationJob = viewModelScope.launch {
            var offset = 0.0
            while (isActive) {
                offset += 0.001   // simulate ambulance movement for demo
                try {
                    api.updateLocation(
                        "Bearer $token",
                        UpdateLocationRequest(
                            latitude = baseLat + offset,
                            longitude = baseLng + offset
                        )
                    )
                    Log.d("GPS", "Location posted: lat=${baseLat + offset}")
                } catch (e: Exception) {
                    Log.e("GPS", "Location post failed: ${e.message}")
                }
                delay(30_000L)   // wait 30 seconds before next post
            }
        }
    }

    fun startLocationUpdatesForCurrentJob() {
        viewModelScope.launch {
            val job = _state.value.job ?: return@launch
            val token = tokenManager.token.firstOrNull() ?: return@launch

            startLocationUpdates(
                token   = token,
                baseLat = job.receivingHospitalLatitude,
                baseLng = job.receivingHospitalLongitude
            )
        }
    }

    override fun onCleared() {
        super.onCleared()
        locationJob?.cancel()   // prevent coroutine leak when screen leaves
    }

}
