using System.Security.Claims;
using LoopGame.Application.Dtos;
using LoopGame.Application.IServices.EconomyAndProgressionServices;
using LoopGame.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace LoopGame.Tests.Services;

public class EconomySecurityTests
{
    private readonly Mock<IEconomyService> _economyMock = new();
    private readonly Mock<IShopService> _shopMock = new();
    private readonly Mock<ISahmService> _sahmMock = new();

    private static ControllerContext CreateUserContext(string userId, string? role = null)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId)
        };
        if (!string.IsNullOrEmpty(role))
        {
            claims.Add(new(ClaimTypes.Role, role));
        }

        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);

        return new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = principal }
        };
    }

    [Fact]
    public async Task GetBalance_WhenAuthenticatedAsDifferentPlayer_ReturnsForbid()
    {
        var controller = new EconomyController(_economyMock.Object)
        {
            ControllerContext = CreateUserContext("42") // Authenticated as player 42
        };

        // Player 42 attempting to view balance of player 100
        var result = await controller.GetBalance(100, CancellationToken.None);

        Assert.IsType<ForbidResult>(result.Result);
        _economyMock.Verify(e => e.GetBalanceAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetBalance_WhenAuthenticatedAsSamePlayer_ReturnsOk()
    {
        _economyMock.Setup(e => e.GetBalanceAsync(42, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(new BalanceDto(500m, 500m, 0m, "Intern")));

        var controller = new EconomyController(_economyMock.Object)
        {
            ControllerContext = CreateUserContext("42")
        };

        var result = await controller.GetBalance(42, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var balance = Assert.IsType<BalanceDto>(ok.Value);
        Assert.Equal(500m, balance.Balance);
    }

    [Fact]
    public async Task GetBalance_WhenAdmin_AllowsAccessToAnyPlayer()
    {
        _economyMock.Setup(e => e.GetBalanceAsync(100, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(new BalanceDto(250m, 250m, 0m, "Intern")));

        var controller = new EconomyController(_economyMock.Object)
        {
            ControllerContext = CreateUserContext("1", role: "Admin")
        };

        var result = await controller.GetBalance(100, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var balance = Assert.IsType<BalanceDto>(ok.Value);
        Assert.Equal(250m, balance.Balance);
    }

    [Fact]
    public async Task ShopPurchase_WhenAuthenticatedAsDifferentPlayer_ReturnsForbid()
    {
        var controller = new ShopController(_shopMock.Object)
        {
            ControllerContext = CreateUserContext("10")
        };

        var result = await controller.Purchase(playerId: 20, itemId: 5, CancellationToken.None);

        Assert.IsType<ForbidResult>(result.Result);
        _shopMock.Verify(s => s.PurchaseItemAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SahmRequestHint_WhenAuthenticatedAsDifferentPlayer_ReturnsForbid()
    {
        var controller = new SahmController(_sahmMock.Object)
        {
            ControllerContext = CreateUserContext("10")
        };

        var result = await controller.RequestHint(
            playerId: 20,
            new HintRequestDto(1, "Practice", ConceptTag: "Arrays"),
            CancellationToken.None);

        Assert.IsType<ForbidResult>(result.Result);
        _sahmMock.Verify(s => s.RequestHintAsync(It.IsAny<int>(), It.IsAny<HintRequestDto>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    private static ControllerContext CreateUnauthenticatedContext() => new()
    {
        HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity()) }
    };

    [Fact]
    public async Task GetBalance_WhenUnauthenticated_ReturnsForbid_AndDoesNotCallService()
    {
        var controller = new EconomyController(_economyMock.Object)
        {
            ControllerContext = CreateUnauthenticatedContext()
        };

        var result = await controller.GetBalance(42, CancellationToken.None);

        Assert.IsType<ForbidResult>(result.Result);
        _economyMock.Verify(e => e.GetBalanceAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetTransactions_WhenUnauthenticated_ReturnsForbid_AndDoesNotCallService()
    {
        var controller = new EconomyController(_economyMock.Object)
        {
            ControllerContext = CreateUnauthenticatedContext()
        };

        var result = await controller.GetTransactions(42, 1, 20, CancellationToken.None);

        Assert.IsType<ForbidResult>(result.Result);
        _economyMock.Verify(e => e.GetTransactionHistoryAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task PayShiftSalary_WhenUnauthenticated_ReturnsForbid_AndDoesNotCallService()
    {
        var controller = new EconomyController(_economyMock.Object)
        {
            ControllerContext = CreateUnauthenticatedContext()
        };

        var result = await controller.PayShiftSalary(42, 1, CancellationToken.None);

        Assert.IsType<ForbidResult>(result.Result);
        _economyMock.Verify(e => e.PayShiftSalaryAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ShopPurchase_WhenUnauthenticated_ReturnsForbid_AndDoesNotCallService()
    {
        var controller = new ShopController(_shopMock.Object)
        {
            ControllerContext = CreateUnauthenticatedContext()
        };

        var result = await controller.Purchase(42, 5, CancellationToken.None);

        Assert.IsType<ForbidResult>(result.Result);
        _shopMock.Verify(s => s.PurchaseItemAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ShopGetCatalog_WhenUnauthenticated_ReturnsForbid_AndDoesNotCallService()
    {
        var controller = new ShopController(_shopMock.Object)
        {
            ControllerContext = CreateUnauthenticatedContext()
        };

        var result = await controller.GetCatalog(42, CancellationToken.None);

        Assert.IsType<ForbidResult>(result.Result);
        _shopMock.Verify(s => s.GetCatalogAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SahmRequestHint_WhenUnauthenticated_ReturnsForbid_AndDoesNotCallService()
    {
        var controller = new SahmController(_sahmMock.Object)
        {
            ControllerContext = CreateUnauthenticatedContext()
        };

        var result = await controller.RequestHint(
            playerId: 42,
            new HintRequestDto(1, "Practice", ConceptTag: "Arrays"),
            CancellationToken.None);

        Assert.IsType<ForbidResult>(result.Result);
        _sahmMock.Verify(s => s.RequestHintAsync(It.IsAny<int>(), It.IsAny<HintRequestDto>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SahmGetStatus_WhenUnauthenticated_ReturnsForbid_AndDoesNotCallService()
    {
        var controller = new SahmController(_sahmMock.Object)
        {
            ControllerContext = CreateUnauthenticatedContext()
        };

        var result = await controller.GetStatus(42, CancellationToken.None);

        Assert.IsType<ForbidResult>(result.Result);
        _sahmMock.Verify(s => s.GetStatusAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // ── Functional Controller Endpoint Tests (All 9 Endpoints) ──

    [Fact]
    public async Task GetTransactions_WhenAuthorized_ReturnsOkWithPagedResult()
    {
        var expected = new PagedResult<TransactionDto>(
            [new TransactionDto(1, 500m, TransactionType.Salary, "Shift salary", 500m, DateTime.UtcNow)],
            1, 20, false);

        _economyMock.Setup(e => e.GetTransactionHistoryAsync(42, 1, 20, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(expected));

        var controller = new EconomyController(_economyMock.Object)
        {
            ControllerContext = CreateUserContext("42")
        };

        var result = await controller.GetTransactions(42, 1, 20, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var paged = Assert.IsType<PagedResult<TransactionDto>>(ok.Value);
        Assert.Single(paged.Items);
    }

    [Fact]
    public async Task ApplyDelta_WhenAdmin_ReturnsOkWithUpdatedBalance()
    {
        _economyMock.Setup(e => e.ApplyEgpDeltaAsync(42, 100m, TransactionType.Bonus, "Manual bonus", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(600m));

        var controller = new EconomyController(_economyMock.Object)
        {
            ControllerContext = CreateUserContext("1", role: "Admin")
        };

        var request = new EconomyController.ApplyEgpDeltaRequest(100m, TransactionType.Bonus, "Manual bonus", null);
        var result = await controller.ApplyDelta(42, request, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Equal(600m, ok.Value);
    }

    [Fact]
    public async Task PayShiftSalary_WhenAuthorized_ReturnsOkWithCreditedAmount()
    {
        _economyMock.Setup(e => e.PayShiftSalaryAsync(42, 1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(750m));

        var controller = new EconomyController(_economyMock.Object)
        {
            ControllerContext = CreateUserContext("42")
        };

        var result = await controller.PayShiftSalary(42, 1, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Equal(750m, ok.Value);
    }

    [Fact]
    public async Task PayShiftSalary_WhenAlreadyPaid_ReturnsConflictOrBadRequest()
    {
        _economyMock.Setup(e => e.PayShiftSalaryAsync(42, 1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<decimal>(EconomyErrors.SalaryAlreadyPaid));

        var controller = new EconomyController(_economyMock.Object)
        {
            ControllerContext = CreateUserContext("42")
        };

        var result = await controller.PayShiftSalary(42, 1, CancellationToken.None);

        Assert.IsAssignableFrom<ObjectResult>(result.Result);
    }

    [Fact]
    public async Task ShopGetCatalog_WhenAuthorized_ReturnsOkWithItems()
    {
        IReadOnlyList<ShopItemDto> items = [
            new ShopItemDto(1, "desk_plant", "Desk Plant", "desk_item", "A plant", 100m, null, false, 1)
        ];
        _shopMock.Setup(s => s.GetCatalogAsync(42, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(items));

        var controller = new ShopController(_shopMock.Object)
        {
            ControllerContext = CreateUserContext("42")
        };

        var result = await controller.GetCatalog(42, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var returnedItems = Assert.IsAssignableFrom<IReadOnlyList<ShopItemDto>>(ok.Value);
        Assert.Single(returnedItems);
    }

    [Fact]
    public async Task ShopGetInventory_WhenAuthorized_ReturnsOkWithItems()
    {
        IReadOnlyList<InventoryItemDto> inventory = [
            new InventoryItemDto(10, 1, "desk_plant", "Desk Plant", "desk_item", DateTime.UtcNow, 100m)
        ];
        _shopMock.Setup(s => s.GetInventoryAsync(42, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(inventory));

        var controller = new ShopController(_shopMock.Object)
        {
            ControllerContext = CreateUserContext("42")
        };

        var result = await controller.GetInventory(42, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var returnedItems = Assert.IsAssignableFrom<IReadOnlyList<InventoryItemDto>>(ok.Value);
        Assert.Single(returnedItems);
    }

    [Fact]
    public async Task ShopPurchase_WhenAuthorizedAndSufficientFunds_ReturnsOk()
    {
        var purchaseResult = new PurchaseResultDto(1, "desk_plant", 100m, 400m, null);
        _shopMock.Setup(s => s.PurchaseItemAsync(42, 1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(purchaseResult));

        var controller = new ShopController(_shopMock.Object)
        {
            ControllerContext = CreateUserContext("42")
        };

        var result = await controller.Purchase(42, 1, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var dto = Assert.IsType<PurchaseResultDto>(ok.Value);
        Assert.Equal(400m, dto.NewBalance);
    }

    [Fact]
    public async Task ShopPurchase_WhenInsufficientFunds_ReturnsErrorResult()
    {
        _shopMock.Setup(s => s.PurchaseItemAsync(42, 1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<PurchaseResultDto>(EconomyErrors.InsufficientBalance));

        var controller = new ShopController(_shopMock.Object)
        {
            ControllerContext = CreateUserContext("42")
        };

        var result = await controller.Purchase(42, 1, CancellationToken.None);

        Assert.IsAssignableFrom<ObjectResult>(result.Result);
    }

    [Fact]
    public async Task SahmGetStatus_WhenAuthorized_ReturnsOkWithStatus()
    {
        var status = new SahmStatusDto("Free", 3, 1, 2, DateTime.UtcNow.Date.AddDays(1));
        _sahmMock.Setup(s => s.GetStatusAsync(42, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(status));

        var controller = new SahmController(_sahmMock.Object)
        {
            ControllerContext = CreateUserContext("42")
        };

        var result = await controller.GetStatus(42, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var dto = Assert.IsType<SahmStatusDto>(ok.Value);
        Assert.Equal("Free", dto.Tier);
        Assert.Equal(2, dto.HintsRemaining);
    }

    [Fact]
    public async Task SahmRequestHint_WhenAuthorized_ReturnsOkWithHint()
    {
        var hint = new HintResponseDto("Free", HintLevel.ConceptualNudge, 1, 2, DateTime.UtcNow.Date.AddDays(1), null);
        var request = new HintRequestDto(1, "Practice", ConceptTag: "Arrays");

        _sahmMock.Setup(s => s.RequestHintAsync(42, request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(hint));

        var controller = new SahmController(_sahmMock.Object)
        {
            ControllerContext = CreateUserContext("42")
        };

        var result = await controller.RequestHint(42, request, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var dto = Assert.IsType<HintResponseDto>(ok.Value);
        Assert.Equal(2, dto.HintsRemaining);
    }

    [Fact]
    public async Task SahmRequestHint_WhenLimitReached_ReturnsErrorResult()
    {
        var request = new HintRequestDto(1, "Practice", ConceptTag: "Arrays");
        _sahmMock.Setup(s => s.RequestHintAsync(42, request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<HintResponseDto>(SahmErrors.DailyHintLimitReached));

        var controller = new SahmController(_sahmMock.Object)
        {
            ControllerContext = CreateUserContext("42")
        };

        var result = await controller.RequestHint(42, request, CancellationToken.None);

        Assert.IsAssignableFrom<ObjectResult>(result.Result);
    }
}
