# One-shot MSTest -> xunit.v3 mechanical conversion for Fluence.Wpf.Tests.
# Preserves UTF-8 BOM + LF. Delete after migration.
$root = Join-Path $PSScriptRoot '.'
$files = Get-ChildItem -Path $root -Recurse -Filter *.cs |
    Where-Object { $_.FullName -notmatch '\\(obj|bin)\\' }

$utf8Bom = New-Object System.Text.UTF8Encoding($true)

foreach ($f in $files) {
    $t = [System.IO.File]::ReadAllText($f.FullName)
    $orig = $t

    # using swap
    $t = $t -creplace 'using Microsoft\.VisualStudio\.TestTools\.UnitTesting;', 'using Xunit;'

    # Remove [TestClass] attribute lines (keep surrounding lines intact)
    $t = [regex]::Replace($t, '(?m)^[ \t]*\[TestClass\][ \t]*\n', '')

    # Method-level attributes
    $t = $t -creplace '\[DataTestMethod\]', '[Theory]'
    $t = $t -creplace '\[TestMethod\]', '[Fact]'
    $t = $t -creplace '\[DataRow\(', '[InlineData('
    $t = [regex]::Replace($t, '\[TestCategory\("([^"]*)"\)\]', '[Trait("Category", "$1")]')

    # [Fact] + [Ignore("...")] (either order) -> [Fact(Skip = "...")]
    $t = [regex]::Replace($t, '\[Fact\][ \t]*\n([ \t]*)\[Ignore\("([^"]*)"\)\]', "[Fact(Skip = `"`$2`")]")
    $t = [regex]::Replace($t, '\[Ignore\("([^"]*)"\)\][ \t]*\n([ \t]*)\[Fact\]', "[Fact(Skip = `"`$1`")]")

    # Assert method renames (argument shapes fixed later, compiler-driven)
    $t = $t -creplace 'Assert\.IsTrue\(', 'Assert.True('
    $t = $t -creplace 'Assert\.IsFalse\(', 'Assert.False('
    $t = $t -creplace 'Assert\.IsNull\(', 'Assert.Null('
    $t = $t -creplace 'Assert\.IsNotNull\(', 'Assert.NotNull('
    $t = $t -creplace 'Assert\.AreEqual\(', 'Assert.Equal('
    $t = $t -creplace 'Assert\.AreNotEqual\(', 'Assert.NotEqual('
    $t = $t -creplace 'Assert\.AreSame\(', 'Assert.Same('
    $t = $t -creplace 'Assert\.AreNotSame\(', 'Assert.NotSame('
    $t = $t -creplace 'Assert\.ThrowsExactly<', 'Assert.Throws<'
    $t = $t -creplace 'Assert\.ThrowsException<', 'Assert.Throws<'
    $t = $t -creplace 'Assert\.IsInstanceOfType<', 'Assert.IsType<'
    $t = $t -creplace 'Assert\.IsInstanceOfType\(', 'Assert.IsType('
    $t = $t -creplace 'Assert\.Inconclusive\(', 'Assert.Skip('

    # CollectionAssert -> xunit equivalents (Contains arg order fixed manually)
    $t = $t -creplace 'CollectionAssert\.AreEqual\(', 'Assert.Equal('
    $t = $t -creplace 'CollectionAssert\.AreEquivalent\(', 'Assert.Equivalent('
    $t = $t -creplace 'CollectionAssert\.Contains\(', 'Assert.Contains('

    if ($t -ne $orig) {
        [System.IO.File]::WriteAllText($f.FullName, $t, $utf8Bom)
        Write-Host "converted: $($f.Name)"
    }
}
