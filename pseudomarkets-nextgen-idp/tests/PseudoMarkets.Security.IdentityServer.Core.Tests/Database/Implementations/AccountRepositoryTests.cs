using Aerospike.Client;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using Shouldly;
using PseudoMarkets.Security.IdentityServer.Core.Constants;
using PseudoMarkets.Security.IdentityServer.Core.Database.Implementations;
using PseudoMarkets.Security.IdentityServer.Core.Exceptions;
using PseudoMarkets.Security.IdentityServer.Core.Models;

namespace PseudoMarkets.Security.IdentityServer.Core.Tests.Database.Implementations;

[TestFixture]
public class AccountRepositoryTests
{
    private Mock<IAerospikeClient> _aerospikeClient = null!;
    private AccountRepository _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _aerospikeClient = new Mock<IAerospikeClient>();
        _sut = new AccountRepository(_aerospikeClient.Object, Mock.Of<ILogger<AccountRepository>>());
    }

    [Test]
    public void GetAccount_ShouldReturnNull_WhenRecordDoesNotExist()
    {
        _aerospikeClient.Setup(x => x.Get(It.IsAny<Policy>(), It.IsAny<Key>())).Returns((Record)null!);

        var result = _sut.GetAccount("missing-user");

        result.ShouldBeNull();
    }

    [Test]
    public void GetAccount_ShouldMapRecordToAccount()
    {
        var record = new Record(new Dictionary<string, object>
        {
            [DatabaseConstants.UserIdBin] = 1_234_567_890L,
            [DatabaseConstants.HashedPasswordBin] = "hashed",
            [DatabaseConstants.AccountTypeBin] = AccountTypeConstants.SystemType,
            [DatabaseConstants.ActiveBin] = true,
            [DatabaseConstants.RolesBin] = new List<object> { "VIEW_BALANCES", "UPDATE_BALANCES" }
        }, 0, 0);

        _aerospikeClient.Setup(x => x.Get(It.IsAny<Policy>(), It.IsAny<Key>())).Returns(record);

        var result = _sut.GetAccount("mapped-user");

        result.ShouldNotBeNull();
        result!.LoginId.ShouldBe("mapped-user");
        result.UserId.ShouldBe(1_234_567_890L);
        result.HashedPassword.ShouldBe("hashed");
        result.AccountType.ShouldBe(AccountTypeConstants.SystemType);
        result.IsActive.ShouldBeTrue();
        result.Roles.ShouldBe(["VIEW_BALANCES", "UPDATE_BALANCES"], ignoreOrder: true);
    }

    [Test]
    public void TryReserveUserId_ShouldReturnTrue_WhenReservationSucceeds()
    {
        var result = _sut.TryReserveUserId(1_234_567_890L, "user");

        result.ShouldBeTrue();
        _aerospikeClient.Verify(x => x.Put(It.IsAny<WritePolicy>(), It.IsAny<Key>(), It.IsAny<Bin[]>()), Times.Once);
    }

    [Test]
    public void TryReserveUserId_ShouldReturnFalse_WhenUserIdAlreadyExists()
    {
        _aerospikeClient
            .Setup(x => x.Put(It.IsAny<WritePolicy>(), It.IsAny<Key>(), It.IsAny<Bin[]>()))
            .Throws(CreateAerospikeException(ResultCode.KEY_EXISTS_ERROR));

        var result = _sut.TryReserveUserId(1_234_567_890L, "user");

        result.ShouldBeFalse();
    }

    [Test]
    public void TryReserveUserId_ShouldWrapUnexpectedAerospikeExceptions()
    {
        _aerospikeClient
            .Setup(x => x.Put(It.IsAny<WritePolicy>(), It.IsAny<Key>(), It.IsAny<Bin[]>()))
            .Throws(CreateAerospikeException(ResultCode.SERVER_NOT_AVAILABLE));

        var ex = Should.Throw<IdentityDependencyException>(() => _sut.TryReserveUserId(1_234_567_890L, "user"));

        ex.InnerException.ShouldBeOfType<AerospikeException>();
    }

    [Test]
    public void ReleaseReservedUserId_ShouldCallDelete()
    {
        _sut.ReleaseReservedUserId(1_234_567_890L);

        _aerospikeClient.Verify(x => x.Delete(It.IsAny<WritePolicy>(), It.IsAny<Key>()), Times.Once);
    }

    [Test]
    public void CreateAccount_ShouldWrapAerospikeExceptions()
    {
        _aerospikeClient
            .Setup(x => x.Put(It.IsAny<WritePolicy>(), It.IsAny<Key>(), It.IsAny<Bin[]>()))
            .Throws(CreateAerospikeException(ResultCode.SERVER_NOT_AVAILABLE));

        var ex = Should.Throw<IdentityDependencyException>(() => _sut.CreateAccount(new Account
        {
            LoginId = "user",
            UserId = 1_234_567_890L,
            HashedPassword = "hashed",
            AccountType = AccountTypeConstants.UserType,
            Roles = []
        }));

        ex.InnerException.ShouldBeOfType<AerospikeException>();
    }

    private static AerospikeException CreateAerospikeException(int resultCode)
    {
        var constructors = typeof(AerospikeException).GetConstructors();

        foreach (var constructor in constructors)
        {
            var parameters = constructor.GetParameters();
            if (parameters.Length == 1 && parameters[0].ParameterType == typeof(int))
            {
                return (AerospikeException)constructor.Invoke([resultCode]);
            }

            if (parameters.Length == 2 &&
                parameters[0].ParameterType == typeof(int) &&
                parameters[1].ParameterType == typeof(string))
            {
                return (AerospikeException)constructor.Invoke([resultCode, "failure"]);
            }
        }

        throw new InvalidOperationException("Unable to create AerospikeException for tests.");
    }
}
