global using System;
global using System.Collections.Generic;
global using System.Linq;
global using DotNetEnv;
global using Microsoft.AspNetCore.Mvc;
global using Microsoft.Extensions.Configuration;
global using Microsoft.Extensions.DependencyInjection;
global using Microsoft.Extensions.Hosting;

global using LoopGame.Domain.Abstractions;
global using LoopGame.Domain.Enums;
global using LoopGame.Domain.ValueObjects;
global using LoopGame.Domain.Entities.Assessment;
global using LoopGame.Domain.Entities.Audit;
global using LoopGame.Domain.Entities.Code;
global using LoopGame.Domain.Entities.Economy;
global using LoopGame.Domain.Entities.Identity;
global using LoopGame.Domain.Entities.Narrative;
global using LoopGame.Domain.Entities.Player;
global using LoopGame.Domain.Entities.SideTask;

global using LoopGame.Infrastructure;
global using LoopGame.Infrastructure.Identity;
global using LoopGame.Infrastructure.Persistence;
