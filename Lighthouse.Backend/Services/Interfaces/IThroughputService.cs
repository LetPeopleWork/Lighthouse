﻿using Lighthouse.Backend.Models;

namespace Lighthouse.Backend.Services.Interfaces
{
    public interface IThroughputService
    {
        Task UpdateThroughputForTeam(Team team);
    }
}
