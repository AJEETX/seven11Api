using AutoMapper;
using WebApi.Model;

namespace WebApi.Helpers
{
    public class AutoMapperProfile : Profile
    {
        public AutoMapperProfile()
        {
            CreateMap<User, UserDto>();
            CreateMap<UserDto, User>();
            CreateMap<UserInfo, User>();
            CreateMap<Vehicle,VehicleDto>();
            CreateMap<VehicleDto,Vehicle>();
        }
    }
}