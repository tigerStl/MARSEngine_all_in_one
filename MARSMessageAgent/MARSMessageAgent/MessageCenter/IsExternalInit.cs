// Polyfill for C# 9 "init" accessor on .NET Framework (requires System.Runtime.CompilerServices.IsExternalInit)
namespace System.Runtime.CompilerServices
{
    internal static class IsExternalInit { }
}
