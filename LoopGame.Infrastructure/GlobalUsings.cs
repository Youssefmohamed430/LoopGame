global using System;
global using System.Collections.Generic;
global using System.Collections.Concurrent;
global using System.Linq;
global using System.Linq.Expressions;
global using System.Text.Json;
global using Mapster;
global using Microsoft.EntityFrameworkCore;
global using Microsoft.EntityFrameworkCore.Metadata.Builders;
global using Microsoft.EntityFrameworkCore.Storage;
global using Microsoft.AspNetCore.Identity;
global using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
global using Microsoft.Extensions.Configuration;
global using Microsoft.Extensions.DependencyInjection;

global using Domain;
global using Domain.IRepositries;
global using Infrastructure.Repositories;

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

global using LoopGame.Infrastructure.Identity;
global using LoopGame.Infrastructure.Persistence;