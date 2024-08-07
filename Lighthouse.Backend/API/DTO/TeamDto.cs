﻿using Lighthouse.Backend.Models;

namespace Lighthouse.Backend.API.DTO
{
    public class TeamDto
    {
        public TeamDto(Team team)
        {
            Name = team.Name;
            Id = team.Id;
            FeatureWip = team.FeatureWIP;
        }

        public string Name { get; set; }

        public int Id { get; set; }

        public int FeatureWip { get;set; }

        public List<FeatureDto> Features { get; } = new List<FeatureDto>();

        public List<ProjectDto> Projects { get; } = new List<ProjectDto>();
    }
}
