package com.ipts.ambulance.ui.vitals

import androidx.compose.foundation.background
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.size
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.text.KeyboardOptions
import androidx.compose.foundation.verticalScroll
import androidx.compose.material3.Button
import androidx.compose.material3.ButtonDefaults
import androidx.compose.material3.CircularProgressIndicator
import androidx.compose.material3.OutlinedTextField
import androidx.compose.material3.OutlinedTextFieldDefaults
import androidx.compose.material3.Text
import androidx.compose.material3.TextButton
import androidx.compose.runtime.Composable
import androidx.compose.runtime.LaunchedEffect
import androidx.compose.runtime.collectAsState
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.setValue
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.text.input.KeyboardType
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import androidx.hilt.navigation.compose.hiltViewModel


@Composable
fun VitalsScreen(
    transferRequestId: String,
    onBack:            () -> Unit,
    viewModel:         VitalsViewModel = hiltViewModel()
) {
    val state by viewModel.state.collectAsState()
    var bloodPressure    by remember { mutableStateOf("") }
    var heartRate        by remember { mutableStateOf("") }
    var oxygenSaturation by remember { mutableStateOf("") }
    var glasgowComaScale by remember { mutableStateOf("") }
    var notes            by remember { mutableStateOf("") }

    // Navigate back automatically on success
    LaunchedEffect(state.success) {
        if (state.success) onBack()
    }

    Column(
        modifier = Modifier
            .fillMaxSize()
            .background(Color(0xFF0A1628))
            .verticalScroll(rememberScrollState())
            .padding(24.dp)
    ) {
        TextButton(onClick = onBack) {
            Text("← Back", color = Color(0xFF8BA3C7))
        }
        Spacer(Modifier.height(8.dp))
        Text("Submit Vitals", color = Color.White,
            fontSize = 22.sp, fontWeight = FontWeight.Bold)
        Text("Record patient vitals during transport",
            color = Color(0xFF8BA3C7), fontSize = 13.sp)
        Spacer(Modifier.height(24.dp))

        VitalsField("Blood Pressure (e.g. 120/80)",
            bloodPressure, { bloodPressure = it }, KeyboardType.Ascii)
        Spacer(Modifier.height(14.dp))
        VitalsField("Heart Rate (bpm)",
            heartRate, { heartRate = it }, KeyboardType.Number)
        Spacer(Modifier.height(14.dp))
        VitalsField("SpO2 — Oxygen Saturation (%)",
            oxygenSaturation, { oxygenSaturation = it }, KeyboardType.Number)
        Spacer(Modifier.height(14.dp))
        VitalsField("Glasgow Coma Scale (3–15)",
            glasgowComaScale, { glasgowComaScale = it }, KeyboardType.Number)
        Spacer(Modifier.height(14.dp))

        OutlinedTextField(
            value = notes, onValueChange = { notes = it },
            label = { Text("Notes (optional)",
                color = Color(0xFF8BA3C7)) },
            modifier = Modifier.fillMaxWidth().height(100.dp),
            colors = OutlinedTextFieldDefaults.colors(
                focusedTextColor     = Color.White,
                unfocusedTextColor   = Color.White,
                focusedBorderColor   = Color(0xFF00C2D4),
                unfocusedBorderColor = Color(0xFF1A3A5C))
        )
        Spacer(Modifier.height(12.dp))

        state.error?.let {
            Text(it, color = Color(0xFFFF4D6A), fontSize = 13.sp)
            Spacer(Modifier.height(8.dp))
        }

        Button(
            onClick = {
                viewModel.submitVitals(
                    transferRequestId, bloodPressure,
                    heartRate, oxygenSaturation,
                    glasgowComaScale, notes)
            },
            enabled  = !state.isLoading,
            modifier = Modifier.fillMaxWidth().height(50.dp),
            colors   = ButtonDefaults.buttonColors(
                containerColor = Color(0xFF1E5FBF))
        ) {
            if (state.isLoading)
                CircularProgressIndicator(color = Color.White,
                    strokeWidth = 2.dp,
                    modifier = Modifier.size(22.dp))
            else
                Text("Submit Vitals", fontWeight = FontWeight.Bold)
        }
    }
}

@Composable
fun VitalsField(
    label:          String,
    value:          String,
    onValueChange:  (String) -> Unit,
    keyboardType:   KeyboardType = KeyboardType.Text
) {
    OutlinedTextField(
        value         = value,
        onValueChange = onValueChange,
        label         = { Text(label, color = Color(0xFF8BA3C7)) },
        singleLine    = true,
        keyboardOptions = KeyboardOptions(keyboardType = keyboardType),
        modifier      = Modifier.fillMaxWidth(),
        colors = OutlinedTextFieldDefaults.colors(
            focusedTextColor     = Color.White,
            unfocusedTextColor   = Color.White,
            focusedBorderColor   = Color(0xFF00C2D4),
            unfocusedBorderColor = Color(0xFF1A3A5C))
    )
}
