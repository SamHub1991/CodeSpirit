namespace CodeSpirit.Core.Page;

[AttributeUsage(AttributeTargets.Method)]
public class BeforeLoadAttribute : Attribute { }

[AttributeUsage(AttributeTargets.Method)]
public class AfterLoadAttribute : Attribute { }