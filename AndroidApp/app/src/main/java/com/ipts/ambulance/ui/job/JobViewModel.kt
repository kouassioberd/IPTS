package com.ipts.ambulance.ui.job

import androidx.lifecycle.ViewModel
import androidx.lifecycle.viewModelScope
import com.ipts.ambulance.data.api.CrewApiService
import com.ipts.ambulance.data.model.CrewActiveJobDto
import com.ipts.ambulance.data.storage.TokenManager
import dagger.hilt.android.lifecycle.HiltViewModel
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.firstOrNull
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
}
