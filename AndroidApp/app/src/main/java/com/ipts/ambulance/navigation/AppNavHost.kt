package com.ipts.ambulance.navigation

import androidx.compose.runtime.Composable
import androidx.navigation.NavHostController
import androidx.navigation.NavType
import androidx.navigation.compose.NavHost
import androidx.navigation.compose.composable
import androidx.navigation.navArgument
import com.ipts.ambulance.ui.job.JobScreen
import com.ipts.ambulance.ui.login.LoginScreen
import com.ipts.ambulance.ui.vitals.VitalsScreen

sealed class Screen(val route: String) {
    object Login  : Screen("login")
    object Job    : Screen("job")
    object Vitals : Screen("vitals?transferRequestId={transferRequestId}") {
        fun createRoute(id: String) = "vitals?transferRequestId=$id"
    }
}

@Composable
fun AppNavHost(navController: NavHostController) {
    NavHost(navController,
        startDestination = Screen.Login.route) {

        composable(Screen.Login.route) {
            LoginScreen(onLoginSuccess = {
                navController.navigate(Screen.Job.route) {
                    popUpTo(Screen.Login.route) { inclusive = true }
                }
            })
        }

        composable(Screen.Job.route) {
            JobScreen(
                onNavigateToVitals = { id ->
                navController.navigate(
                    Screen.Vitals.createRoute(id))
                },
                onLogout = {
                    navController.navigate(Screen.Login.route) {
                        // Clear the entire back stack so pressing back
                        // after logout doesn't return to the job screen
                        popUpTo(0) { inclusive = true }
                    }
                }
            )
        }

        composable(
            route     = Screen.Vitals.route,
            arguments = listOf(navArgument("transferRequestId") {
                type = NavType.StringType
                defaultValue = ""
            })
        ) { backStackEntry ->
            val id = backStackEntry.arguments
                ?.getString("transferRequestId") ?: ""
            VitalsScreen(
                transferRequestId = id,
                onBack = { navController.popBackStack() }
            )
        }
    }
}
