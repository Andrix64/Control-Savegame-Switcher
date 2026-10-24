using Lss.App.ViewModels;

namespace Lss.Tests;

/// <summary>
/// Validazione del nome nelle finestre di nuova partita e rinomina.
/// </summary>
public sealed class NewPlaythroughViewModelTests
{
    private static NewPlaythroughViewModel New(string existing = "Luigi", string? currentName = null) =>
        new(name => string.Equals(name, existing, StringComparison.OrdinalIgnoreCase), currentName);

    [Fact]
    public void EmptyName_CannotCreate()
    {
        NewPlaythroughViewModel vm = New();

        Assert.False(vm.CanCreate);
        Assert.Equal(Strings.ProfileNameHint, vm.HintText);
        Assert.False(vm.HintIsError);
    }

    [Fact]
    public void InvalidName_DisablesCreateAndShowsHint()
    {
        NewPlaythroughViewModel vm = New();

        vm.Name = "Mario/Peach";

        Assert.False(vm.CanCreate);
        Assert.True(vm.HintIsError);
        Assert.Equal(Loc.F(Strings.InvalidNameHint, Strings.ProfileNameHint), vm.HintText);
    }

    [Fact]
    public void ExistingName_ShowsProfileExists()
    {
        NewPlaythroughViewModel vm = New();

        vm.Name = "Luigi";

        Assert.False(vm.CanCreate);
        Assert.True(vm.HintIsError);
        Assert.Equal(Strings.ProfileExists, vm.HintText);
    }

    [Fact]
    public void ValidNewName_CanCreate()
    {
        NewPlaythroughViewModel vm = New();

        vm.Name = "  Peach  ";

        Assert.True(vm.CanCreate);
        Assert.Equal(new NewPlaythroughResult("Peach", true, true), vm.CreateResult());
    }

    [Fact]
    public void Rename_SameName_CannotCreate()
    {
        NewPlaythroughViewModel vm = New(currentName: "Luigi");

        Assert.True(vm.IsRename);
        Assert.False(vm.CanCreate);
    }

    [Fact]
    public void Rename_OnlyCaseChanged_CanCreate()
    {
        NewPlaythroughViewModel vm = New(currentName: "Luigi");

        vm.Name = "luigi";

        Assert.True(vm.CanCreate);
        Assert.False(vm.HintIsError);
    }

    [Fact]
    public void EmptyProfile_IsTheOppositeOfCopy()
    {
        NewPlaythroughViewModel vm = New();

        vm.EmptyProfile = true;

        Assert.False(vm.CopyCurrentSaves);
    }
}
