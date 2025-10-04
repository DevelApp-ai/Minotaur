using System.Text.Json;
using Minotaur.UI.Blazor.Models;
using Microsoft.JSInterop;

namespace Minotaur.UI.Blazor.Services;

/// <summary>
/// Service for handling user authentication and session management
/// </summary>
public class AuthenticationService
{
    private readonly HttpClient _httpClient;
    private readonly IJSRuntime _jsRuntime;
    private readonly ILogger<AuthenticationService> _logger;
    private readonly string _authBaseUrl;
    private readonly JsonSerializerOptions _jsonOptions;

    private UserProfile? _currentUser;
    private string? _accessToken;
    
    public event EventHandler<UserProfile?>? UserChanged;

    public AuthenticationService(
        HttpClient httpClient, 
        IJSRuntime jsRuntime, 
        ILogger<AuthenticationService> logger, 
        IConfiguration configuration)
    {
        _httpClient = httpClient;
        _jsRuntime = jsRuntime;
        _logger = logger;
        _authBaseUrl = configuration.GetValue<string>("Authentication:BaseUrl") ?? "https://auth.minotaur.dev/api/v1";
        
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };
    }

    public UserProfile? CurrentUser => _currentUser;
    public bool IsAuthenticated => _currentUser != null && !string.IsNullOrEmpty(_accessToken);

    /// <summary>
    /// Initialize authentication state from stored tokens
    /// </summary>
    public async Task InitializeAsync()
    {
        try
        {
            var token = await GetStoredTokenAsync();
            if (!string.IsNullOrEmpty(token))
            {
                _accessToken = token;
                _httpClient.DefaultRequestHeaders.Authorization = 
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
                
                // Verify token and get user profile
                var user = await GetCurrentUserAsync();
                if (user != null)
                {
                    _currentUser = user;
                    UserChanged?.Invoke(this, _currentUser);
                }
                else
                {
                    // Token is invalid, clear it
                    await ClearStoredTokenAsync();
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize authentication");
        }
    }

    /// <summary>
    /// Authenticate user with email/username and password
    /// </summary>
    public async Task<AuthenticationResult> LoginAsync(LoginRequest request)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync($"{_authBaseUrl}/auth/login", request, _jsonOptions);
            
            if (response.IsSuccessStatusCode)
            {
                var tokenResponse = await response.Content.ReadFromJsonAsync<TokenResponse>(_jsonOptions);
                if (tokenResponse != null && tokenResponse.User != null)
                {
                    await StoreTokenAsync(tokenResponse.AccessToken);
                    _accessToken = tokenResponse.AccessToken;
                    _currentUser = tokenResponse.User;
                    
                    // Set authorization header for future requests
                    _httpClient.DefaultRequestHeaders.Authorization = 
                        new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _accessToken);
                    
                    UserChanged?.Invoke(this, _currentUser);
                    
                    return AuthenticationResult.Success(_currentUser, _accessToken, tokenResponse.RefreshToken);
                }
            }
            
            var errorContent = await response.Content.ReadAsStringAsync();
            _logger.LogWarning("Login failed: {StatusCode}, {Error}", response.StatusCode, errorContent);
            return AuthenticationResult.Failed("Invalid credentials");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Login error");
            return AuthenticationResult.Failed("An error occurred during login");
        }
    }

    /// <summary>
    /// Register new user account
    /// </summary>
    public async Task<AuthenticationResult> RegisterAsync(RegistrationRequest request)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync($"{_authBaseUrl}/auth/register", request, _jsonOptions);
            
            if (response.IsSuccessStatusCode)
            {
                var tokenResponse = await response.Content.ReadFromJsonAsync<TokenResponse>(_jsonOptions);
                if (tokenResponse != null && tokenResponse.User != null)
                {
                    await StoreTokenAsync(tokenResponse.AccessToken);
                    _accessToken = tokenResponse.AccessToken;
                    _currentUser = tokenResponse.User;
                    
                    _httpClient.DefaultRequestHeaders.Authorization = 
                        new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _accessToken);
                    
                    UserChanged?.Invoke(this, _currentUser);
                    
                    return AuthenticationResult.Success(_currentUser, _accessToken, tokenResponse.RefreshToken);
                }
            }
            
            var errorContent = await response.Content.ReadAsStringAsync();
            _logger.LogWarning("Registration failed: {StatusCode}, {Error}", response.StatusCode, errorContent);
            return AuthenticationResult.Failed("Registration failed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Registration error");
            return AuthenticationResult.Failed("An error occurred during registration");
        }
    }

    /// <summary>
    /// Login with social provider (GitHub, Google, Microsoft)
    /// </summary>
    public async Task<AuthenticationResult> LoginWithProviderAsync(string provider, string code, string redirectUri)
    {
        try
        {
            var socialRequest = new
            {
                Code = code,
                RedirectUri = redirectUri
            };

            var response = await _httpClient.PostAsJsonAsync($"{_authBaseUrl}/auth/social/{provider}", socialRequest, _jsonOptions);
            
            if (response.IsSuccessStatusCode)
            {
                var tokenResponse = await response.Content.ReadFromJsonAsync<TokenResponse>(_jsonOptions);
                if (tokenResponse != null && tokenResponse.User != null)
                {
                    await StoreTokenAsync(tokenResponse.AccessToken);
                    _accessToken = tokenResponse.AccessToken;
                    _currentUser = tokenResponse.User;
                    
                    _httpClient.DefaultRequestHeaders.Authorization = 
                        new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _accessToken);
                    
                    UserChanged?.Invoke(this, _currentUser);
                    
                    return AuthenticationResult.Success(_currentUser, _accessToken, tokenResponse.RefreshToken);
                }
            }
            
            return AuthenticationResult.Failed("Social login failed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Social login error for provider: {Provider}", provider);
            return AuthenticationResult.Failed("Social login failed");
        }
    }

    /// <summary>
    /// Log out current user
    /// </summary>
    public async Task LogoutAsync()
    {
        try
        {
            if (!string.IsNullOrEmpty(_accessToken))
            {
                // Notify server about logout
                await _httpClient.DeleteAsync($"{_authBaseUrl}/auth/logout");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Logout error");
        }
        finally
        {
            // Clear local state regardless of server response
            await ClearStoredTokenAsync();
            _accessToken = null;
            _currentUser = null;
            _httpClient.DefaultRequestHeaders.Authorization = null;
            
            UserChanged?.Invoke(this, null);
        }
    }

    /// <summary>
    /// Get current user profile
    /// </summary>
    public async Task<UserProfile?> GetCurrentUserAsync()
    {
        if (string.IsNullOrEmpty(_accessToken))
            return null;

        try
        {
            var response = await _httpClient.GetAsync($"{_authBaseUrl}/user/profile");
            
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<UserProfile>(_jsonOptions);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get current user");
        }
        
        return null;
    }

    /// <summary>
    /// Update user profile
    /// </summary>
    public async Task<bool> UpdateProfileAsync(UserProfile profile)
    {
        if (!IsAuthenticated)
            return false;

        try
        {
            var response = await _httpClient.PutAsJsonAsync($"{_authBaseUrl}/user/profile", profile, _jsonOptions);
            
            if (response.IsSuccessStatusCode)
            {
                _currentUser = profile;
                UserChanged?.Invoke(this, _currentUser);
                return true;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update user profile");
        }
        
        return false;
    }

    /// <summary>
    /// Refresh access token
    /// </summary>
    public async Task<bool> RefreshTokenAsync()
    {
        try
        {
            var refreshToken = await GetStoredRefreshTokenAsync();
            if (string.IsNullOrEmpty(refreshToken))
                return false;

            var refreshRequest = new { RefreshToken = refreshToken };
            var response = await _httpClient.PostAsJsonAsync($"{_authBaseUrl}/auth/refresh", refreshRequest, _jsonOptions);
            
            if (response.IsSuccessStatusCode)
            {
                var tokenResponse = await response.Content.ReadFromJsonAsync<TokenResponse>(_jsonOptions);
                if (tokenResponse != null)
                {
                    await StoreTokenAsync(tokenResponse.AccessToken);
                    _accessToken = tokenResponse.AccessToken;
                    
                    _httpClient.DefaultRequestHeaders.Authorization = 
                        new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _accessToken);
                    
                    return true;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Token refresh failed");
        }
        
        return false;
    }

    #region Private Methods

    private async Task StoreTokenAsync(string token)
    {
        try
        {
            await _jsRuntime.InvokeVoidAsync("localStorage.setItem", "minotaur_access_token", token);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to store access token");
        }
    }

    private async Task<string?> GetStoredTokenAsync()
    {
        try
        {
            return await _jsRuntime.InvokeAsync<string?>("localStorage.getItem", "minotaur_access_token");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get stored token");
            return null;
        }
    }

    private async Task<string?> GetStoredRefreshTokenAsync()
    {
        try
        {
            return await _jsRuntime.InvokeAsync<string?>("localStorage.getItem", "minotaur_refresh_token");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get stored refresh token");
            return null;
        }
    }

    private async Task ClearStoredTokenAsync()
    {
        try
        {
            await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", "minotaur_access_token");
            await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", "minotaur_refresh_token");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to clear stored tokens");
        }
    }

    #endregion

    #region Helper Classes

    private class TokenResponse
    {
        public string AccessToken { get; set; } = "";
        public string? RefreshToken { get; set; }
        public UserProfile? User { get; set; }
    }

    #endregion
}