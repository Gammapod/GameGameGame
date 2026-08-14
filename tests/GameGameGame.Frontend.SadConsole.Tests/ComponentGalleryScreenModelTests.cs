using GameGameGame.Frontend.SadConsole;

namespace GameGameGame.Frontend.SadConsole.Tests;

public sealed class ComponentGalleryScreenModelTests
{
    [Fact]
    public void ComponentGalleryIncludesSelectorAndToastPopupExamples()
    {
        var model = new ComponentGalleryScreenModel();

        Assert.Contains(model.Examples, example => example.Kind == ComponentGalleryExampleKind.SelectorPopup && example.Id == "selector-popup");
        Assert.Contains(model.Examples, example => example.Kind == ComponentGalleryExampleKind.ToastPopup && example.Id == "toast-popup");
    }

    [Fact]
    public void ComponentGallerySelectorPopupExampleUsesModalFocusForMouse()
    {
        var model = new ComponentGalleryScreenModel();

        var open = model.Handle(ComponentGalleryCommand.Select);
        var clickOther = model.SelectExample(1);
        var hoverOther = model.HoverExample(1);

        Assert.Equal(ComponentGalleryResultKind.SelectorPopupRequested, open.Kind);
        Assert.True(model.SelectorPopupOpen);
        Assert.Equal(0, model.SelectedIndex);
        Assert.Null(model.HoveredIndex);
        Assert.Contains("focused", clickOther.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("focused", hoverOther.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ComponentGallerySelectorPopupCancelRestoresGalleryListFocus()
    {
        var model = new ComponentGalleryScreenModel();
        model.Handle(ComponentGalleryCommand.Select);

        var close = model.Handle(ComponentGalleryCommand.Cancel);
        var selectToast = model.SelectExample(1);

        Assert.Equal(ComponentGalleryResultKind.Stay, close.Kind);
        Assert.False(model.SelectorPopupOpen);
        Assert.Equal(ComponentGalleryResultKind.ToastRequested, selectToast.Kind);
    }

    [Fact]
    public void ComponentGalleryToastExampleCreatesFourSecondToast()
    {
        var model = new ComponentGalleryScreenModel(selectedIndex: 1);

        var result = model.Handle(ComponentGalleryCommand.Select);
        var toast = model.CreateToastExample();

        Assert.Equal(ComponentGalleryResultKind.ToastRequested, result.Kind);
        Assert.Equal(TimeSpan.FromSeconds(4), toast.Duration);
        Assert.Contains("Toast popup example", toast.Rows);
    }
}
