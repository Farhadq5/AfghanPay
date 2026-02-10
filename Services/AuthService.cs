using AfghanPay.API.Data;
using AfghanPay.API.DTOs;
using AfghanPay.API.Models;
using AfghanPay.API.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace AfghanPay.API.Services
{
    public class AuthService : IAuthService
    {
        private readonly AppDbContext _dbcontext;
        private readonly IConfiguration _configuration;

        public AuthService(AppDbContext context, IConfiguration configuration)
        {
            _configuration = configuration;
            _dbcontext = context;
        }


     
        public string GenerateJwtToken(Guid userId, string role, string? agentCode = null)
        {
            var jwtSettings = _configuration.GetSection("Jwt");
            var secretKey = jwtSettings["SecretKey"];
            var issuer = jwtSettings["issuer"];
            var asudience = jwtSettings["audience"];
            var expiryMinutes = int.Parse(jwtSettings["ExpiryMinutes"] ?? "1440");

            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);
            
            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()),
                new Claim(ClaimTypes.Role, role),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            //add agent specific claim
            if (!string.IsNullOrEmpty(agentCode))
            {
                claims.Add(new Claim("AgentCode", agentCode));
            }

            var token = new JwtSecurityToken
                (
                issuer: issuer,
                audience: asudience,
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(expiryMinutes),
                signingCredentials: credentials
                );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        // admin login
        public async Task<LoginResponse> AdminLoginAsync(AdminLoginRequest request)
        {
            try
            {
                //finding the admin user
                var adminUser = await _dbcontext.AdminUsers.FirstOrDefaultAsync
                    (a => a.Username == request.UserName);

                if ( adminUser == null)
                {
                    return new LoginResponse
                        {
                        Success = false,
                        Message = "Admin user not found."
                    };
                }

                // verify password
                if (request.Password != adminUser.PasswordHash)
                {
                    return new LoginResponse
                    {
                        Success = false,
                        Message = "Invalid credentials."
                    };
                }

                // generate jwt token
                var token = GenerateJwtToken(adminUser.Id, "admin");

                return new LoginResponse
                {
                    Success = true,
                    Message = "Login successful.",
                    Token = token,
                    User = new UserInfo
                    {
                        Id = adminUser.Id,
                        FullName = adminUser.FullName,
                        PhoneNumber = adminUser.Username,
                        Balance = 0,
                        IsAgent = false
                    }
                };
            }
            catch (Exception ex)
            {
                return new LoginResponse
                {
                    Success = false,
                    Message = $"Login failed: {ex.Message}"
                };
            }
        }

        public async Task<LoginResponse> UserLoginAsync(LoginRequest request)
        {
            try
            {
                //find user by phone number
                var user = await _dbcontext.Users.FirstOrDefaultAsync(
                    u => u.PhoneNumber == request.PhoneNumber);
                if (user == null)
                {
                    return new LoginResponse
                    {
                        Success = false,
                        Message = "User not found or inactive."
                    };
                }

                //verify pin
                if (request.Pin != user.PinHash)
                {
                    return new LoginResponse
                    {
                        Success = false,
                        Message = "Invalid credentials."
                    };
                }

                // if user suspended
                if (user.Status == "suspended")
                {
                    return new LoginResponse
                    {
                        Success = false,
                        Message = "User account is suspended."
                    };
                }
                //if user is agent
                var agent = await _dbcontext.Agents.FirstOrDefaultAsync(a => a.UserId == user.Id && a.Status == "active");

                var isagent = agent != null;
                var role = isagent ? "agent" : "user";

                // generate jwt token
                var token = GenerateJwtToken(user.Id, role, agent?.AgentCode);

                return new LoginResponse
                {
                    Success = true,
                    Message = "Login successful.",
                    Token = token,
                    User = new UserInfo
                    {
                        Id = user.Id,
                        FullName = user.FullName,
                        PhoneNumber = user.PhoneNumber,
                        Balance = user.Balance,
                        IsAgent = isagent,
                        AgentCode = agent?.AgentCode
                    }
                };
            }
            catch (Exception ex)
            {
                return new LoginResponse
                {
                    Success = false,
                    Message = $"An error occurred during login: {ex.Message}"
                };
            }
        }

        public async Task<RegisterResponse> RegisterUserAsync(RegisterRequest request)
        {
            try
            {
                //validate input
                if (string.IsNullOrEmpty(request.PhoneNumber))
                {
                    return new RegisterResponse
                    {
                        Success = false,
                        Message = "Phone number is required."
                    };
                }

                if (string.IsNullOrEmpty(request.FullName))
                {
                    return new RegisterResponse
                    {
                        Success = false,
                        Message = "Full name is required."
                    };
                }

                if (string.IsNullOrEmpty(request.Pin))
                {
                    return new RegisterResponse
                    {
                        Success = false,
                        Message = "PIN is required."
                    };
                }

                if (request.Pin.Length != 4)
                {
                    return new RegisterResponse
                    {
                        Success = false,
                        Message = "PIN Pin must be 4 digits"
                    };
                }
               // var pinDigits = Convert.ToInt32(request.Pin);
               // var confirmPinDigits = Convert.ToInt32(request.ConfirmPin);

                if  (!request.Pin.All(char.IsDigit))
                {
                    return new RegisterResponse
                    {
                        Success = false,
                        Message = "PIN must contain only digits."
                    };
                }
               
                if ( request.Pin != request.ConfirmPin)
                {
                    return new RegisterResponse
                    {
                        Success = false,
                        Message = "PIN and Confirm PIN do not match."
                    };
                }

                //Normalize phone number (remove spaces, add country code if missing)
                var phoneNumber = request.PhoneNumber.Trim().Replace(" ", "");
                if (!phoneNumber.StartsWith("+93"))
                {
                    phoneNumber = "+93" + phoneNumber;
                }

                //check if user already exists
                var existingUser = await _dbcontext.Users
                    .FirstOrDefaultAsync(u => u.PhoneNumber == phoneNumber);

                if (existingUser != null)
                    {
                    return new RegisterResponse
                    {
                        Success = false,
                        Message = "User with this phone number already exists."
                    };
                }
               

                // create new user
                var user = new User
                {
                    Id = Guid.NewGuid(),
                    PhoneNumber = phoneNumber,
                    FullName = request.FullName,
                    PinHash = request.Pin.ToString(),
                    Balance = 0.00m,
                    Status = "active",
                    CreatedAt = DateTime.UtcNow
                };

                _dbcontext.Users.Add(user);
                await _dbcontext.SaveChangesAsync();

                return new RegisterResponse
                {
                    Success = true,
                    Message = "User registered successfully.",
                    User = new UserInfo
                    {
                        Id = user.Id,
                        FullName = user.FullName,
                        PhoneNumber = user.PhoneNumber,
                        Balance = user.Balance,
                        IsAgent = false
                    }
                };

            }
            catch (Exception ex)
            {
                return new RegisterResponse
                {
                    Success = false,
                    Message = $"Registration failed: {ex.Message}"
                };
            }
        }
    }
}
