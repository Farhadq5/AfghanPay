using AfghanPay.API.DTOs;
using AfghanPay.API.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace AfghanPay.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly ILogger<AuthController> _logger;

        public AuthController(IAuthService authService, ILogger<AuthController> logger)
        {
            _authService = authService;
            _logger = logger;
        }

        [HttpPost("user/login")]
        [ProducesResponseType(typeof(LoginResponse), 200)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> UserLogin([FromBody] LoginRequest request)
        {
            try
            {
                _logger.LogInformation("Login attempt for phone: {Phone}", request.PhoneNumber);

                var response = await _authService.UserLoginAsync(request);

                if (!response.Success)
                {
                    _logger.LogWarning("Login failed for phone: {Phone}. Reason: {Reason}",
                        request.PhoneNumber, response.Message);
                    return BadRequest(response);
                }

                _logger.LogInformation("Login successful for phone: {Phone}", request.PhoneNumber);
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during user login");
                return StatusCode(500, new LoginResponse
                {
                    Success = false,
                    Message = "An error occurred during login"
                });
            }
        }

        /// <summary>
        /// Register new user account
        /// </summary>
        /// <param name="request">Phone number, full name, PIN</param>
        /// <returns>Registration result</returns>

        [HttpPost("register")]
        [ProducesResponseType(typeof(RegisterResponse), 200)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            try
            {
                _logger.LogInformation("Registration attempt for phone: {Phone}", request.PhoneNumber);

                var response = await _authService.RegisterUserAsync(request);

                if (!response.Success)
                {
                    _logger.LogWarning("Registration failed for phone: {Phone}. Reason: {Reason}",
                        request.PhoneNumber, response.Message);
                    return BadRequest(response);
                }

                _logger.LogInformation("Registration successful for phone: {Phone}", request.PhoneNumber);
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during registration");
                return StatusCode(500, new RegisterResponse
                {
                    Success = false,
                    Message = "An error occurred during registration"
                });
            }
        }

        /// <summary>
        /// Admin Login (Admin Panel)
        /// </summary>
        /// <param name="request">Username and password</param>
        /// <returns>JWT token and admin info</returns>
        [HttpPost("admin/login")]
        [ProducesResponseType(typeof(LoginResponse), 200)]
        [ProducesResponseType(400)]

        public async Task<IActionResult> AdminLogin([FromBody] AdminLoginRequest request)
        {
            try
            {
                _logger.LogInformation("Admin login attempt for username:{UserName}", request.UserName);
                var response = await _authService.AdminLoginAsync(request);
                if (!response.Success)
                {
                    _logger.LogWarning("Admin login failed for username:{UserName}. Reason: {Reason}",
                        request.UserName, response.Message);
                    return BadRequest(response);
                }
                _logger.LogInformation("Admin login successful for username:{UserName}", request.UserName);
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during admin login");
                return StatusCode(500, new LoginResponse
                {
                    Success = false,
                    Message = "An error occurred during admin login"
                });
            }
        }
    }
}
