package com.ipts.ambulance.di

import android.content.Context
import com.ipts.ambulance.data.api.CrewApiService
import com.ipts.ambulance.data.storage.TokenManager
import dagger.Module
import dagger.Provides
import dagger.hilt.InstallIn
import dagger.hilt.android.qualifiers.ApplicationContext
import dagger.hilt.components.SingletonComponent
import retrofit2.Retrofit
import retrofit2.converter.gson.GsonConverterFactory
import javax.inject.Singleton

@Module
@InstallIn(SingletonComponent::class)
object AppModule {

    // Replace 192.168.X.X with your machine's actual IP or your backend deployment url
    private const val BASE_URL = "https://ipts-api.up.railway.app/api/"


    @Provides
    @Singleton
    fun provideRetrofit(): Retrofit = Retrofit.Builder()
        .baseUrl(BASE_URL)
        .addConverterFactory(GsonConverterFactory.create())
        .build()

    @Provides @Singleton
    fun provideCrewApiService(retrofit: Retrofit): CrewApiService =
        retrofit.create(CrewApiService::class.java)

    @Provides @Singleton
    fun provideTokenManager(
        @ApplicationContext context: Context
    ): TokenManager = TokenManager(context)
}
