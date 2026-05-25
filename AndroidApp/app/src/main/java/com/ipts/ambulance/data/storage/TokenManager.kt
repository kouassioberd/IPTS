package com.ipts.ambulance.data.storage

import android.content.Context
import androidx.datastore.preferences.core.edit
import androidx.datastore.preferences.core.stringPreferencesKey
import androidx.datastore.preferences.preferencesDataStore
import kotlinx.coroutines.flow.Flow
import kotlinx.coroutines.flow.map

private val Context.dataStore
        by preferencesDataStore("crew_prefs")

class TokenManager(private val context: Context) {

    companion object {
        val TOKEN_KEY        = stringPreferencesKey("access_token")
        val AMBULANCE_ID_KEY = stringPreferencesKey("ambulance_id")
        val CREW_ID_KEY      = stringPreferencesKey("crew_id")
        val FULL_NAME_KEY    = stringPreferencesKey("full_name")
        val ROLE_KEY         = stringPreferencesKey("role")
        val UNIT_KEY         = stringPreferencesKey("ambulance_unit")
    }

    val token: Flow<String?> =
        context.dataStore.data.map { it[TOKEN_KEY] }
    val ambulanceId: Flow<String?> =
        context.dataStore.data.map { it[AMBULANCE_ID_KEY] }
    val fullName:    Flow<String?> =
        context.dataStore.data.map { it[FULL_NAME_KEY] }
    val role:        Flow<String?> =
        context.dataStore.data.map { it[ROLE_KEY] }
    val unit:        Flow<String?> =
        context.dataStore.data.map { it[UNIT_KEY] }

    suspend fun save(
        token:       String,
        ambulanceId: String,
        crewId:      String,
        fullName:    String,
        role:        String,
        unit:        String
    ) {
        context.dataStore.edit {
            it[TOKEN_KEY]        = token
            it[AMBULANCE_ID_KEY] = ambulanceId
            it[CREW_ID_KEY]      = crewId
            it[FULL_NAME_KEY]    = fullName
            it[ROLE_KEY]         = role
            it[UNIT_KEY]         = unit
        }
    }

    suspend fun clear() {
        context.dataStore.edit { it.clear() }
    }
}
