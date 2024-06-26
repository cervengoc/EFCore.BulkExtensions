﻿using System.Threading.Tasks;
using EFCore.BulkExtensions.SqlAdapters;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace EFCore.BulkExtensions.Tests.Owned;

public class NestedOwnedTests
{
    [Theory]
    [InlineData(SqlType.SqlServer)]
    [InlineData(SqlType.PostgreSql)]
    [InlineData(SqlType.MySql)]
    public async Task NestedOwnedTest(SqlType sqlType)
    {
        ContextUtil.DatabaseType = sqlType;
        using var context = new NestedDbContext(ContextUtil.GetOptions<NestedDbContext>(databaseName: $"{nameof(EFCoreBulkTest)}_NestedOwned"));

        context.Database.EnsureDeleted();
        context.Database.EnsureCreated();

        NestedRoot[] entities = [
            new()
            {
                NestedRootId = "nestedrootid",
                FirstNested = new()
                {
                    FirstNestedProperty = "firstnested",
                    FirstNestedPropertyEnumType = EnumType.B,
                    SecondNested = new()
                    {
                        SecondNestedProperty = "secondnested",
                        ThirdNested = new()
                        {
                            ThirdNestedProperty = "thirdnested",
                        },
                    },
                },
            }
        ];

        await context.BulkInsertOrUpdateAsync(entities);

        var nestedroot = await context.Set<NestedRoot>().SingleAsync();
        Assert.Equal("nestedrootid", nestedroot.NestedRootId);
        Assert.Equal("firstnested", nestedroot.FirstNested.FirstNestedProperty);
        Assert.Equal(EnumType.B, nestedroot.FirstNested.FirstNestedPropertyEnumType);
        Assert.Equal("secondnested", nestedroot.FirstNested.SecondNested.SecondNestedProperty);
        Assert.Equal("thirdnested", nestedroot.FirstNested.SecondNested.ThirdNested.ThirdNestedProperty);
    }
}

public enum EnumType
{
    A = 1,
    B = 2,
}

public class NestedRoot
{
    public string NestedRootId { get; set; } = default!;
    public FirstNested FirstNested { get; set; } = default!;
}

public class FirstNested
{
    public string? FirstNestedProperty { get; set; }
    public EnumType FirstNestedPropertyEnumType { get; set; }
    public SecondNested SecondNested { get; set; } = default!;
}

public class SecondNested
{
    public string? SecondNestedProperty { get; set; }
    public ThirdNested ThirdNested { get; set; } = default!;
}

public class ThirdNested
{
    public string? ThirdNestedProperty { get; set; }
}

public class NestedDbContext(DbContextOptions opts) : DbContext(opts)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<NestedRoot>(r => r.OwnsOne(r => r.FirstNested, f => f.OwnsOne(f => f.SecondNested, s => s.OwnsOne(s => s.ThirdNested))));
    }
}
