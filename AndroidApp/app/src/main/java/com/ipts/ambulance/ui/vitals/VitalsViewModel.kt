package com.ipts.ambulance.ui.vitals

import androidx.lifecycle.ViewModel
import androidx.lifecycle.viewModelScope
import com.ipts.ambulance.data.api.CrewApiService
import com.ipts.ambulance.data.model.SubmitVitalsRequest
import com.ipts.ambulance.data.storage.TokenManager
import dagger.hilt.android.lifecycle.HiltViewModel
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.firstOrNull
import kotlinx.coroutines.launch
import javax.inject.Inject

data class VitalsUiState(
    val isLoading: Boolean = false,
    val error:     String? = null,
    val success:   Boolean = false
)

@HiltViewModel
class VitalsViewModel @Inject constructor(
    private val api: CrewApiService,
    private val tokenManager: TokenManager
) : ViewModel() {

    private val _state = MutableStateFlow(VitalsUiState())
    val state: StateFlow<VitalsUiState> = _state

    fun submitVitals(
        transferRequestId: String,
        bloodPressure:     String,
        heartRate:         String,
        oxygenSaturation:  String,
        glasgowComaScale:  String,
        notes:             String
    ) {
        val hr  = heartRate.toIntOrNull()
        val spo = oxygenSaturation.toIntOrNull()
        val gcs = glasgowComaScale.toIntOrNull()

        // Validate
        when {
            bloodPressure.isBlank() || hr==null || spo==null || gcs==null ->
                _state.value = VitalsUiState(
                    error = "All fields required. HR, SpO2, GCS must be numbers.")
            hr < 0 || hr > 300 ->
                _state.value = VitalsUiState(
                    error = "Heart rate must be 0–300.")
            spo < 0 || spo > 100 ->
                _state.value = VitalsUiState(
                    error = "SpO2 must be 0–100.")
            gcs < 3 || gcs > 15 ->
                _state.value = VitalsUiState(
                    error = "Glasgow Coma Scale must be 3–15.")
            else -> viewModelScope.launch {
                _state.value = VitalsUiState(isLoading = true)
                android.util.Log.d("VITALS_DEBUG", "transferRequestId = '$transferRequestId'")
                val token = tokenManager.token.firstOrNull()
                if (token == null) {
                    _state.value = VitalsUiState(error = "Not logged in.")
                    return@launch
                }
                try {
                    val response = api.submitVitals(
                        "Bearer $token",
                        SubmitVitalsRequest(
                            transferRequestId = transferRequestId,
                            bloodPressure = bloodPressure,
                            heartRate = hr,
                            oxygenSaturation = spo,
                            glasgowComaScale = gcs,
                            notes = notes.ifBlank { "None" }
                        )
                    )
                    if (response.isSuccessful)
                        _state.value = VitalsUiState(success = true)
                    else
                        _state.value = VitalsUiState(
                            error = "Failed: HTTP ${response.code()}")
                } catch (e: Exception) {
                    _state.value = VitalsUiState(
                        error = "Cannot connect to server.")
                }
            }
        }
    }
}
