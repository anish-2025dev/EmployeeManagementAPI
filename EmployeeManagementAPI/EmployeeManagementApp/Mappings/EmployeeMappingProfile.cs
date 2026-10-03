using AutoMapper;
using EmployeeManagementApp.DTOs;
using EmployeeManagementApp.Models;

namespace EmployeeManagementApp.Mappings
{
    public class EmployeeMappingProfile : Profile
    {
        public EmployeeMappingProfile()
        {
            CreateMap<CreateEmployeeDTO, Employee>();

            CreateMap<UpdateEmployeeDTO, Employee>();

            CreateMap<Employee, ResponseEmployeeDTO>();
        }
    }
}