using System;

namespace CoreOne.OQuery.Tests.Fixtures;

public class TestEntity
{
    public string Name { get; set; } = string.Empty;
    public int Age { get; set; }
    public bool IsActive { get; set; }
    public Guid Id { get; set; }
    public DateTime CreatedAt { get; set; }
    public decimal Score { get; set; }
    public string? Department { get; set; }
}
