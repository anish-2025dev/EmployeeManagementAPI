using AutoMapper;
using EmployeeManagementApp.DTOs;
using EmployeeManagementApp.Models;

namespace EmployeeManagementApp.Mappings
{
    public class UserMappingProfile : Profile
    {
        public UserMappingProfile()
        {
            CreateMap<RegisterUserDTO, User>();

            CreateMap<User, ResponseUserDTO>();
        }
    }
}