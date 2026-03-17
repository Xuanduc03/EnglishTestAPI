using App.Application.DTOs;
using App.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace App.Application.Leaderboards.Queries
{
    public record GetLeaderboardQuery(int limit = 10) : IRequest<List<LeaderboardDto>>;

   
}
