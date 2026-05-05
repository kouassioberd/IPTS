package com.ipts.ambulance.ui.login

import androidx.lifecycle.ViewModel
import androidx.lifecycle.viewModelScope
import com.ipts.ambulance.data.api.CrewApiService
import com.ipts.ambulance.data.model.CrewLoginRequest
import com.ipts.ambulance.data.storage.TokenManager
import dagger.hilt.android.lifecycle.HiltViewModel
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.launch
import javax.inject.Inject

data class LoginUiState(
    val isLoading: Boolean = false,
    val error:     String? = null,
    val success:   Boolean = false
)

@HiltViewModel
class LoginViewModel @Inject constructor(
    private val api: CrewApiService,
    private val tokenManager: TokenManager
) : ViewModel() {

    private val _state = MutableStateFlow(LoginUiState())
    val state: StateFlow<LoginUiState> = _state

    fun login(email: String, password: String) {
        if (email.isBlank() || password.isBlank()) {
            _state.value = LoginUiState(
                error = "Email and password are required.")
            return
        }
        viewModelScope.launch {
            _state.value = LoginUiState(isLoading = true)
            try {
                val response = api.login(
                    CrewLoginRequest(email.trim(), password)
                )
                if (response.isSuccessful && response.body() != null) {
                    val body = response.body()!!
                    tokenManager.save(
                        token       = body.accessToken,
                        ambulanceId = body.ambulanceId,
                        crewId      = body.crewId,
                        fullName    = body.fullName,
                        role        = body.role,
                        unit        = body.ambulanceUnit
                    )
                    _state.value = LoginUiState(success = true)
                } else {
                    _state.value = LoginUiState(
                        error = "Invalid email or password.")
                }
            } catch (e: Exception) {
                _state.value = LoginUiState(
                    error = "Cannot connect to server. Check your IP.")
            }
        }
    }
}

