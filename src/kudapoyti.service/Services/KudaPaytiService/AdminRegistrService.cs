﻿using AutoMapper;
using kudapoyti.DataAccess.DbConstexts;
using kudapoyti.DataAccess.Interfaces;
using kudapoyti.Domain.Entities.Admins;
using kudapoyti.Service.Common.Exceptions;
using kudapoyti.Service.Common.Security;
using kudapoyti.Service.Dtos.AdminAccountDtos;
using kudapoyti.Service.Interfaces;
using kudapoyti.Service.Interfaces.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace kudapoyti.Service.Services.KudaPaytiService
{
    public class AdminRegistrService:IAdminRegistrService
    {
        private readonly IUnitOfWork _work;

        public AdminRegistrService(IUnitOfWork repository)
        {
            this._work = repository;

        }        

        public async Task<bool> RegisterAsync(AdminRegisterDto registerDto)
        {
            var emailcheck = await _work.Admins.FirstOrDefaoultAsync(x => x.Email == registerDto.Email);
            if (emailcheck is not null)
                throw new StatusCodeException(HttpStatusCode.Conflict, "Email alredy exist");
            var hasherResult = PasswordHasher.Hash(registerDto.Password);
            var admin = (Admin1)registerDto;
            admin.PasswordHash = hasherResult.passwordHash;
            admin.Salt = hasherResult.salt;
            _work.Admins.CreateAsync(admin);
            var databaseResult = await _work.SaveChangesAsync();
            return databaseResult > 0;
        }
    }
}
