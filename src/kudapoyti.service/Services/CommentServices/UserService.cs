﻿using kudapoyti.Service.Common.Exceptions;
using kudapoyti.Service.Common.Security;
using kudapoyti.Service.Dtos.AccountDTOs;
using kudapoyti.Service.Interfaces.CommentServices;
using System.Net;

namespace kudapoyti.Service.Services.CommentServices
{
    public class UserService : IUserService
    {
        private readonly IEmailService _emailService;
        private readonly ICacheService cacheService;
        private readonly IAuthManager _authManager;

        public UserService(IEmailService emailService, ICacheService cacheService, IAuthManager authManager)
        {
            _emailService = emailService;
            this.cacheService = cacheService;
            _authManager = authManager;
        }
        public async Task LoginAsync(UserValidateDto userValidate)
        {
            try
            {
                await _emailService.SendAsync(userValidate);
            }
            catch
            {
                throw new NotFoundException(HttpStatusCode.BadRequest, "Something went wrong");
            }
        }
        public async Task<(bool, string)> VerifyCodeAsync(string email, string code)
        {
            try
            {
                var realCode = await cacheService.GetValueAsync(email);
                if (realCode.Item1 != null)
                {
                    if (realCode.Item1 == code)
                        return (true, _authManager.GenerateToken(realCode.Item2));
                    else if (realCode.Item2 == null)
                        throw new NotFoundException(HttpStatusCode.Gone, "Code time limit expired");
                    else
                        throw new NotFoundException(HttpStatusCode.Forbidden, "Verification code is wrong");
                }
                throw new Exception("There aren't any request of login");
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
    }
}
