package com.ipts.ambulance.ui.job

import androidx.compose.foundation.background
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.width
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.verticalScroll
import androidx.compose.material3.Button
import androidx.compose.material3.ButtonDefaults
import androidx.compose.material3.Card
import androidx.compose.material3.CardDefaults
import androidx.compose.material3.CircularProgressIndicator
import androidx.compose.material3.OutlinedButton
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.runtime.collectAsState
import androidx.compose.runtime.getValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import androidx.hilt.navigation.compose.hiltViewModel

// TransferStatus: 0=Confirmed, 1=AmbulanceAssigned, 2=EnRoute,
//                 3=PatientOnBoard, 4=InTransit, 5=Delivered, 6=Cancelled
private val STATUS_LABELS = mapOf(
    0 to "Confirmed",        1 to "Ambulance Assigned",
    2 to "En Route",          3 to "Patient On Board",
    4 to "In Transit",        5 to "Delivered",
    6 to "Cancelled"
)

@Composable
fun JobScreen(
    onNavigateToVitals: (String) -> Unit,
    viewModel: JobViewModel = hiltViewModel()
) {
    val state by viewModel.state.collectAsState()

    Column(
        modifier = Modifier
            .fillMaxSize()
            .background(Color(0xFF0A1628))
            .verticalScroll(rememberScrollState())
            .padding(24.dp)
    ) {
        // Header
        Row(verticalAlignment = Alignment.CenterVertically) {
            Text("🚑", fontSize = 28.sp)
            Spacer(Modifier.width(10.dp))
            Column {
                Text("Active Job", color = Color.White,
                    fontSize = 22.sp, fontWeight = FontWeight.Bold)
                Text("IPTS Ambulance Crew",
                    color = Color(0xFF8BA3C7), fontSize = 13.sp)
            }
        }
        Spacer(Modifier.height(24.dp))

        when {
            state.isLoading -> Box(
                Modifier.fillMaxWidth(), Alignment.Center) {
                CircularProgressIndicator(color = Color(0xFF00C2D4))
            }
            state.noJob -> NoJobCard { viewModel.loadJob() }
            state.error != null -> {
                Text(state.error!!, color = Color(0xFFFF4D6A))
                Spacer(Modifier.height(12.dp))
                Button(onClick = { viewModel.loadJob() },
                    colors = ButtonDefaults.buttonColors(
                        containerColor = Color(0xFF1E5FBF))) {
                    Text("Retry")
                }
            }
            state.job != null -> {
                val job = state.job!!

                Card(colors = CardDefaults.cardColors(
                    containerColor = Color(0xFF112240)),
                    modifier = Modifier.fillMaxWidth()) {
                    Column(Modifier.padding(20.dp)) {
                        // Status
                        Text("Status",
                            color = Color(0xFF8BA3C7), fontSize = 12.sp)
                        Text(STATUS_LABELS[job.status] ?: "Unknown",
                            color = Color(0xFF00C2D4), fontSize = 20.sp,
                            fontWeight = FontWeight.Bold)
                        Spacer(Modifier.height(20.dp))

                        InfoRow("From",      job.sendingHospitalName)
                        Spacer(Modifier.height(12.dp))
                        InfoRow("To",        job.receivingHospitalName)
                        Spacer(Modifier.height(12.dp))
                        InfoRow("Address",   job.receivingHospitalAddress)
                        Spacer(Modifier.height(12.dp))
                        InfoRow("Ambulance", job.ambulanceUnit)
                        if (job.hasVitalsSubmitted) {
                            Spacer(Modifier.height(12.dp))
                            Text("✓ Vitals submitted",
                                color = Color(0xFF00D68F), fontSize = 13.sp)
                        }
                    }
                }
                Spacer(Modifier.height(16.dp))

                OutlinedButton(
                    onClick = {
                        onNavigateToVitals(job.transferRequestId)
                    },
                    modifier = Modifier.fillMaxWidth(),
                    border = androidx.compose.foundation
                        .BorderStroke(1.dp, Color(0xFF00C2D4))
                ) {
                    Text("📋 Submit Vitals",
                        color = Color(0xFF00C2D4),
                        fontWeight = FontWeight.SemiBold)
                }
                Spacer(Modifier.height(8.dp))

                Button(onClick = { viewModel.loadJob() },
                    modifier = Modifier.fillMaxWidth(),
                    colors = ButtonDefaults.buttonColors(
                        containerColor = Color(0xFF1A3A5C))) {
                    Text("🔄 Refresh", color = Color.White)
                }
            }
        }
    }
}

@Composable
fun NoJobCard(onRefresh: () -> Unit) {
    Card(colors = CardDefaults.cardColors(
        containerColor = Color(0xFF112240)),
        modifier = Modifier.fillMaxWidth()) {
        Column(Modifier.padding(24.dp),
            horizontalAlignment = Alignment.CenterHorizontally) {
            Text("📭", fontSize = 48.sp)
            Spacer(Modifier.height(12.dp))
            Text("No active job assigned.",
                color = Color(0xFF8BA3C7), fontSize = 16.sp)
            Spacer(Modifier.height(16.dp))
            Button(onClick = onRefresh,
                colors = ButtonDefaults.buttonColors(
                    containerColor = Color(0xFF1E5FBF))) {
                Text("Refresh")
            }
        }
    }
}

@Composable
fun InfoRow(label: String, value: String) {
    Column {
        Text(label,
            color = Color(0xFF8BA3C7), fontSize = 11.sp)
        Text(value, color = Color.White, fontSize = 14.sp,
            fontWeight = FontWeight.Medium)
    }
}
