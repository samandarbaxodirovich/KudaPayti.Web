﻿using AutoMapper;
using kudapoyti.DataAccess.DbConstexts;
using kudapoyti.DataAccess.Interfaces;
using kudapoyti.Service.Interfaces;
using kudapoyti.Service.ViewModels;
using kudapoyti.Domain.Entities.Places;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.ConstrainedExecution;
using System.Text;
using System.Threading.Tasks;
using kudapoyti.Service.Common.Helpers;
using kudapoyti.Service.Common.Exceptions;
using System.Net;
using kudapoyti.Service.Dtos;
using kudapoyti.Service.Interfaces.Common;
using kudapoyti.Service.Common.Utils;
using kudapoyti.Service.Services.Common;
using kudapoyti.DataAccess.Repositories;
using Microsoft.EntityFrameworkCore;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;
using Microsoft.AspNetCore.Hosting;
using Microsoft.OpenApi.Writers;

namespace kudapoyti.Service.Services.KudaPaytiService;

public class PlaceService : IPlaceService
{
    private readonly IPaginationService _paginator;
    private readonly IUnitOfWork _repository;
    private readonly IImageService _imageService;

    public PlaceService(IUnitOfWork unitOfWork,IImageService imageService ,IPaginationService paginatorService)
    {
        this._paginator = paginatorService;
        this._repository = unitOfWork;

        this._imageService = imageService;
    }
    public async Task<bool> CreateAsync(PlaceCreateDto dto)
    {
        var entity = (Place)dto;
        entity.rank = 0;
        entity.rankedUsersCount = 0;
        entity.Ranked_point = 0;
        entity.ImageUrl = await _imageService.SaveImageAsync(dto.Image!);
        entity.CreatedAt = TimeHelper.GetCurrentServerTime();
        _repository.Places.CreateAsync(entity);
        var result = await _repository.SaveChangesAsync();
        return result > 0;
    }

    public async Task<bool> DeleteAsync(long id)
    {
        var entity = await _repository.Places.FindByIdAsync(id);
        if (entity is not null)
        {
            _repository.Places.DeleteAsync(id);
            var result = await _repository.SaveChangesAsync();
            return result > 0;
        }
        else throw new NotFoundException(HttpStatusCode.NotFound, "Place is not found.");
    }

    
    public async Task<PlaceViewModel> GetAsync(long id)
    { 
        var place = await _repository.Places.FindByIdAsync(id);
        if (place is not null)
        {
            var res = (PlaceViewModel)place;
            return res;
        }
        else throw new NotFoundException(HttpStatusCode.NotFound, "Place is not found");
    }
    public async Task<IEnumerable<PlaceViewModel>> GetByKeyword(string keyword)
    {
        IEnumerable<PlaceViewModel> places = await _repository.Places
            .Where(x=>x.Title.ToLower().Contains(keyword.ToLower())
            || x.Description.ToLower().Contains(keyword.ToLower())
            || x.Region.ToLower().Contains(keyword.ToLower()))
            .Select(x =>(PlaceViewModel)x).ToListAsync();
        if (places.Count() != 0) return places;
        else throw new NotFoundException(HttpStatusCode.NotFound, $"No info has been found related to {keyword}");
    }
    public async Task<IEnumerable<PlaceViewModel>> GetByCityAsync(string cityName)
    {
        IEnumerable<PlaceViewModel> places = await _repository.Places
            .Where(x=>x.Region.ToLower().Contains(cityName.ToLower()))
            .Select(x => (PlaceViewModel)x).ToListAsync();
        if (places.Count() != 0) return places;
        else throw new NotFoundException(HttpStatusCode.NotFound, $"No info has been found related to {cityName}");
    }
    public async Task<bool> UpdateAsync(long id, PlaceUpdateDto updateDto)
    {
        var place = await _repository.Places.FindByIdAsync(id);
        if (place is null) throw new NotFoundException(HttpStatusCode.NotFound, "Place is not found");
        _repository.Entry<Place>(place!).State = EntityState.Detached;
        var updatePlace = (Place)updateDto;
        if (updateDto.Image is not null)
        {
            await _imageService.DeleteImageAsync(place.ImageUrl!);
            updatePlace.ImageUrl = await _imageService.SaveImageAsync(updateDto.Image);
        }
        updatePlace.Id = id;
        updatePlace.CreatedAt = TimeHelper.GetCurrentServerTime();
        _repository.Places.UpdateAsync(id, updatePlace);
        var result = await _repository.SaveChangesAsync();
        return result > 0;
    }
    public async Task<bool> AddRankPoint(long placeId, int rank)
    {
        var place = await _repository.Places.FindByIdAsync(placeId);
        if (place is null) throw new NotFoundException(HttpStatusCode.NotFound, "Place is not found");
        _repository.Entry<Place>(place!).State = EntityState.Detached;
        place.rankedUsersCount += 1;
        place.Ranked_point += rank;
        place.rank = place.Ranked_point/ place.rankedUsersCount;

        _repository.Places.UpdateAsync(placeId, place);
        var result = await _repository.SaveChangesAsync();
        return result > 0;
    }

    public async Task<IEnumerable<PlaceViewModel>> GetTopPLacesAsync()
    {
        return await _repository.Places.GetAll().OrderByDescending(x => x)
            .Take(10).Select(x =>(PlaceViewModel)x).ToListAsync();
    }

    public async Task<IEnumerable<PlaceViewModel>> GetByTypeAsync(PaginationParams @paginationParams,string type)
    {
        var query = _repository.Places.GetAll().Where(x => x.PlaceSiteUrl == $"{type}").Select(x => (PlaceViewModel)x);
        return await _paginator.ToPagedAsync(query, @paginationParams.PageNumber, @paginationParams.PageSize);
    }
    public async Task<IEnumerable<string>> GetOtherTypes()
    {
        var alreadyHavetypes = new List<string> { "Отели", "Развлечения", "Рестораны", "Рассказы о путешествиях", "Авиабилеты" };
        return await _repository.Places.GetAll().Where(x=>!alreadyHavetypes.Contains(x.PlaceSiteUrl))
            .Select(x=>x.PlaceSiteUrl).ToListAsync();
    }

     public async Task<IEnumerable<PlaceBaseViewModel>> GetAllAsync(PaginationParams @params)
    {
        var query = from product in _repository.Places.GetAll().OrderBy(x => x.rank)
                    select new PlaceBaseViewModel()
                    {
                        Id = product.Id,
                        ImageUrl = product.ImageUrl,
                        Title = product.Title,
                        Region = product.Region,
                        rank = product.rank,
                        Description=product.Description,
                        PlaceSiteUrl=product.PlaceSiteUrl,
                        rankedUsersCount=product.rankedUsersCount
                    };
        return await query.Skip((@params.PageNumber - 1) * @params.PageSize)
                          .Take(@params.PageSize).AsNoTracking()
                          .ToListAsync();
    }
    
}
