/*
 * Copyright 2026 Dan Cunningham
 *
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the following conditions are met:
 *
 * 1. Redistributions of source code must retain the above copyright notice,
 *    this list of conditions and the following disclaimer.
 * 2. Redistributions in binary form must reproduce the above copyright notice,
 *    this list of conditions and the following disclaimer in the documentation
 *    and/or other materials provided with the distribution.
 * 3. Neither the name of the copyright holder nor the names of its contributors
 *    may be used to endorse or promote products derived from this software
 *    without specific prior written permission.
 *
 * THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS"
 * AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE
 * IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE
 * ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER OR CONTRIBUTORS BE
 * LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR
 * CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF
 * SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS
 * INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN
 * CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE)
 * ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF
 * THE POSSIBILITY OF SUCH DAMAGE.
 */

using Fluence.Wpf.Automation;
using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace Fluence.Wpf.Controls
{
    /// <summary>
    /// A modal dialog with a title area, arbitrary body content, and up to three command
    /// buttons, mirroring the WinUI 3 <c>ContentDialog</c> control. While open the dialog sits
    /// above a smoke layer that dims and blocks everything behind it: over a window that exposes
    /// a full-window overlay host (a FluenceWindow <c>PART_DialogOverlayHost</c>) the smoke
    /// covers the entire window, title bar included; over a plain window it is hosted in the
    /// content adorner layer. A tunneling input guard additionally blocks any press outside the
    /// dialog. The owner's visual tree is never restructured. The body uses the inherited
    /// <see cref="ContentControl.Content"/> and <see cref="ContentControl.ContentTemplate"/>.
    /// </summary>
    [TemplatePart(Name = PART_PrimaryButton, Type = typeof(ButtonBase))]
    [TemplatePart(Name = PART_SecondaryButton, Type = typeof(ButtonBase))]
    [TemplatePart(Name = PART_CloseButton, Type = typeof(ButtonBase))]
    public class ContentDialog : ContentControl
    {
        // Template part names.
        private const string PART_PrimaryButton = "PART_PrimaryButton";
        private const string PART_SecondaryButton = "PART_SecondaryButton";
        private const string PART_CloseButton = "PART_CloseButton";

        // Name of the optional full-window overlay host panel a window template may expose
        // (FluenceWindow does) so the dialog can dim and block the entire window, title bar
        // included, instead of only the content adorner layer.
        private const string DialogOverlayHostPart = "PART_DialogOverlayHost";

        /// <summary>
        /// Initializes static members of the ContentDialog class and overrides the default
        /// style metadata so the control picks up its Fluent template from Generic.xaml.
        /// </summary>
        static ContentDialog()
        {
            DefaultStyleKeyProperty.OverrideMetadata(
                typeof(ContentDialog),
                new FrameworkPropertyMetadata(typeof(ContentDialog)));
        }

        /// <summary>
        /// Identifies the <see cref="Title"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty TitleProperty =
            DependencyProperty.Register(
                nameof(Title),
                typeof(object),
                typeof(ContentDialog),
                new FrameworkPropertyMetadata(null));

        /// <summary>
        /// Gets or sets the title shown at the top of the dialog.
        /// </summary>
        public object? Title
        {
            get => GetValue(TitleProperty);
            set => SetValue(TitleProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="TitleTemplate"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty TitleTemplateProperty =
            DependencyProperty.Register(
                nameof(TitleTemplate),
                typeof(DataTemplate),
                typeof(ContentDialog),
                new FrameworkPropertyMetadata(null));

        /// <summary>
        /// Gets or sets the template used to display <see cref="Title"/>.
        /// </summary>
        public DataTemplate? TitleTemplate
        {
            get => (DataTemplate?)GetValue(TitleTemplateProperty);
            set => SetValue(TitleTemplateProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="PrimaryButtonText"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty PrimaryButtonTextProperty =
            DependencyProperty.Register(
                nameof(PrimaryButtonText),
                typeof(string),
                typeof(ContentDialog),
                new FrameworkPropertyMetadata(string.Empty, null, CoerceButtonText));

        /// <summary>
        /// Gets or sets the text of the primary button. The button is collapsed while the
        /// text is empty.
        /// </summary>
        public string PrimaryButtonText
        {
            get => (string)GetValue(PrimaryButtonTextProperty);
            set => SetValue(PrimaryButtonTextProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="SecondaryButtonText"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty SecondaryButtonTextProperty =
            DependencyProperty.Register(
                nameof(SecondaryButtonText),
                typeof(string),
                typeof(ContentDialog),
                new FrameworkPropertyMetadata(string.Empty, null, CoerceButtonText));

        /// <summary>
        /// Gets or sets the text of the secondary button. The button is collapsed while the
        /// text is empty.
        /// </summary>
        public string SecondaryButtonText
        {
            get => (string)GetValue(SecondaryButtonTextProperty);
            set => SetValue(SecondaryButtonTextProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="CloseButtonText"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty CloseButtonTextProperty =
            DependencyProperty.Register(
                nameof(CloseButtonText),
                typeof(string),
                typeof(ContentDialog),
                new FrameworkPropertyMetadata(string.Empty, null, CoerceButtonText));

        /// <summary>
        /// Gets or sets the text of the close button. The button is collapsed while the
        /// text is empty.
        /// </summary>
        public string CloseButtonText
        {
            get => (string)GetValue(CloseButtonTextProperty);
            set => SetValue(CloseButtonTextProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="DefaultButton"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty DefaultButtonProperty =
            DependencyProperty.Register(
                nameof(DefaultButton),
                typeof(ContentDialogButton),
                typeof(ContentDialog),
                new FrameworkPropertyMetadata(ContentDialogButton.None));

        /// <summary>
        /// Gets or sets which command button receives initial keyboard focus when the dialog
        /// opens and is invoked by the Enter key while focus is not on another command button.
        /// </summary>
        public ContentDialogButton DefaultButton
        {
            get => (ContentDialogButton)GetValue(DefaultButtonProperty);
            set => SetValue(DefaultButtonProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="IsPrimaryButtonEnabled"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty IsPrimaryButtonEnabledProperty =
            DependencyProperty.Register(
                nameof(IsPrimaryButtonEnabled),
                typeof(bool),
                typeof(ContentDialog),
                new FrameworkPropertyMetadata(true));

        /// <summary>
        /// Gets or sets a value indicating whether the primary button is enabled.
        /// </summary>
        public bool IsPrimaryButtonEnabled
        {
            get => (bool)GetValue(IsPrimaryButtonEnabledProperty);
            set => SetValue(IsPrimaryButtonEnabledProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="IsSecondaryButtonEnabled"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty IsSecondaryButtonEnabledProperty =
            DependencyProperty.Register(
                nameof(IsSecondaryButtonEnabled),
                typeof(bool),
                typeof(ContentDialog),
                new FrameworkPropertyMetadata(true));

        /// <summary>
        /// Gets or sets a value indicating whether the secondary button is enabled.
        /// </summary>
        public bool IsSecondaryButtonEnabled
        {
            get => (bool)GetValue(IsSecondaryButtonEnabledProperty);
            set => SetValue(IsSecondaryButtonEnabledProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="PrimaryButtonCommand"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty PrimaryButtonCommandProperty =
            DependencyProperty.Register(
                nameof(PrimaryButtonCommand),
                typeof(ICommand),
                typeof(ContentDialog),
                new FrameworkPropertyMetadata(null));

        /// <summary>
        /// Gets or sets the command executed when the primary button is invoked and the
        /// <see cref="PrimaryButtonClick"/> event is not canceled.
        /// </summary>
        public ICommand? PrimaryButtonCommand
        {
            get => (ICommand?)GetValue(PrimaryButtonCommandProperty);
            set => SetValue(PrimaryButtonCommandProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="PrimaryButtonCommandParameter"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty PrimaryButtonCommandParameterProperty =
            DependencyProperty.Register(
                nameof(PrimaryButtonCommandParameter),
                typeof(object),
                typeof(ContentDialog),
                new FrameworkPropertyMetadata(null));

        /// <summary>
        /// Gets or sets the parameter passed to <see cref="PrimaryButtonCommand"/>.
        /// </summary>
        public object? PrimaryButtonCommandParameter
        {
            get => GetValue(PrimaryButtonCommandParameterProperty);
            set => SetValue(PrimaryButtonCommandParameterProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="SecondaryButtonCommand"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty SecondaryButtonCommandProperty =
            DependencyProperty.Register(
                nameof(SecondaryButtonCommand),
                typeof(ICommand),
                typeof(ContentDialog),
                new FrameworkPropertyMetadata(null));

        /// <summary>
        /// Gets or sets the command executed when the secondary button is invoked and the
        /// <see cref="SecondaryButtonClick"/> event is not canceled.
        /// </summary>
        public ICommand? SecondaryButtonCommand
        {
            get => (ICommand?)GetValue(SecondaryButtonCommandProperty);
            set => SetValue(SecondaryButtonCommandProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="SecondaryButtonCommandParameter"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty SecondaryButtonCommandParameterProperty =
            DependencyProperty.Register(
                nameof(SecondaryButtonCommandParameter),
                typeof(object),
                typeof(ContentDialog),
                new FrameworkPropertyMetadata(null));

        /// <summary>
        /// Gets or sets the parameter passed to <see cref="SecondaryButtonCommand"/>.
        /// </summary>
        public object? SecondaryButtonCommandParameter
        {
            get => GetValue(SecondaryButtonCommandParameterProperty);
            set => SetValue(SecondaryButtonCommandParameterProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="CloseButtonCommand"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty CloseButtonCommandProperty =
            DependencyProperty.Register(
                nameof(CloseButtonCommand),
                typeof(ICommand),
                typeof(ContentDialog),
                new FrameworkPropertyMetadata(null));

        /// <summary>
        /// Gets or sets the command executed when the close button is invoked (or the dialog
        /// is dismissed with the Escape key) and the <see cref="CloseButtonClick"/> event is
        /// not canceled.
        /// </summary>
        public ICommand? CloseButtonCommand
        {
            get => (ICommand?)GetValue(CloseButtonCommandProperty);
            set => SetValue(CloseButtonCommandProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="CloseButtonCommandParameter"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty CloseButtonCommandParameterProperty =
            DependencyProperty.Register(
                nameof(CloseButtonCommandParameter),
                typeof(object),
                typeof(ContentDialog),
                new FrameworkPropertyMetadata(null));

        /// <summary>
        /// Gets or sets the parameter passed to <see cref="CloseButtonCommand"/>.
        /// </summary>
        public object? CloseButtonCommandParameter
        {
            get => GetValue(CloseButtonCommandParameterProperty);
            set => SetValue(CloseButtonCommandParameterProperty, value);
        }

        /// <summary>
        /// Occurs when the primary button is invoked. Set
        /// <see cref="ContentDialogButtonClickEventArgs.Cancel"/> to <see langword="true"/>
        /// to keep the dialog open and skip <see cref="PrimaryButtonCommand"/>.
        /// </summary>
        public event EventHandler<ContentDialogButtonClickEventArgs>? PrimaryButtonClick;

        /// <summary>
        /// Occurs when the secondary button is invoked. Set
        /// <see cref="ContentDialogButtonClickEventArgs.Cancel"/> to <see langword="true"/>
        /// to keep the dialog open and skip <see cref="SecondaryButtonCommand"/>.
        /// </summary>
        public event EventHandler<ContentDialogButtonClickEventArgs>? SecondaryButtonClick;

        /// <summary>
        /// Occurs when the close button is invoked or the dialog is dismissed with the
        /// Escape key. Set <see cref="ContentDialogButtonClickEventArgs.Cancel"/> to
        /// <see langword="true"/> to keep the dialog open and skip <see cref="CloseButtonCommand"/>.
        /// </summary>
        public event EventHandler<ContentDialogButtonClickEventArgs>? CloseButtonClick;

        /// <summary>
        /// Occurs after the dialog has been added to the owner window's adorner layer.
        /// </summary>
        public event EventHandler? Opened;

        /// <summary>
        /// Occurs after the dialog has been removed from the owner window's adorner layer.
        /// </summary>
        public event EventHandler? Closed;

        /// <summary>
        /// Shows the dialog modally over the active window (or the application main window)
        /// and returns a task that completes with the <see cref="ContentDialogResult"/> once
        /// the dialog closes. While open, a smoke layer dims the owner window content and
        /// blocks mouse input, Tab navigation is trapped inside the dialog, Escape dismisses
        /// as if the close button were invoked, and Enter invokes <see cref="DefaultButton"/>.
        /// Must be called on the dialog's dispatcher thread.
        /// </summary>
        /// <returns>A task that completes with the dialog result when the dialog closes.</returns>
        /// <exception cref="InvalidOperationException">
        /// The dialog is already open, no owner window could be resolved, the owner window has
        /// no <see cref="UIElement"/> content root, or no adorner layer exists above the owner
        /// window content.
        /// </exception>
        public Task<ContentDialogResult> ShowAsync()
        {
            if (_showCompletionSource is not null)
            {
                throw new InvalidOperationException(
                    "This ContentDialog is already open. Wait for the pending ShowAsync task to complete before showing it again.");
            }

            Window owner = ResolveOwnerWindow();

            // Prefer a full-window overlay host: a FluenceWindow exposes PART_DialogOverlayHost,
            // a panel that spans the title bar and the content, so the dialog dims and blocks the
            // entire window (including title-bar content such as a search box). Fall back to the
            // content adorner layer for plain windows, whose client area carries no extra chrome.
            System.Windows.Controls.Panel? overlayHost =
                (owner as Control)?.Template?.FindName(DialogOverlayHostPart, owner) as System.Windows.Controls.Panel;

            UIElement? adornedContent = null;
            AdornerLayer? adornerLayer = null;
            if (overlayHost is null)
            {
                if (owner.Content is not UIElement ownerRoot)
                {
                    throw new InvalidOperationException(
                        "ContentDialog.ShowAsync requires the owner window to have a UIElement content root to host the modal overlay.");
                }

                adornedContent = ownerRoot;
                adornerLayer = AdornerLayer.GetAdornerLayer(ownerRoot)
                    ?? throw new InvalidOperationException(
                        "ContentDialog.ShowAsync could not find an AdornerLayer above the owner window content. Ensure the window is shown and its template contains an AdornerDecorator.");
            }

            TaskCompletionSource<ContentDialogResult> completionSource = new(TaskCreationOptions.RunContinuationsAsynchronously);
            _showCompletionSource = completionSource;
            _previousFocus = Keyboard.FocusedElement;
            _overlayRoot = BuildOverlayRoot();

            if (overlayHost is not null)
            {
                _overlayHostPanel = overlayHost;
                _ = overlayHost.Children.Add(_overlayRoot);
            }
            else
            {
                _hostLayer = adornerLayer;
                _hostAdorner = new DialogHostAdorner(adornedContent!, _overlayRoot);
                adornerLayer!.Add(_hostAdorner);
            }

            // Defense in depth (and the only block on the adorner path, where the smoke covers
            // just the content): swallow any pointer press in the owner window whose source is
            // not inside the dialog, keeping chrome such as a title-bar search box inert.
            _owner = owner;
            _ownerInputBlocker = OnOwnerPreviewMouseDown;
            owner.AddHandler(PreviewMouseDownEvent, _ownerInputBlocker, handledEventsToo: true);

            // The template (and with it the command buttons) is applied during the layout
            // pass that realizes the adorner, so move initial focus once layout has run.
            _ = Dispatcher.BeginInvoke(DispatcherPriority.Loaded, new Action(MoveInitialFocus));

            Opened?.Invoke(this, EventArgs.Empty);
            return completionSource.Task;
        }

        /// <summary>
        /// Closes the dialog with <see cref="ContentDialogResult.None"/>. Does nothing when
        /// the dialog is not open.
        /// </summary>
        public void Hide()
        {
            CloseDialog(ContentDialogResult.None);
        }

        /// <inheritdoc />
        protected override AutomationPeer OnCreateAutomationPeer()
        {
            return new ContentDialogAutomationPeer(this);
        }

        /// <inheritdoc />
        public override void OnApplyTemplate()
        {
            _primaryButton?.Click -= OnPrimaryButtonClick;
            _secondaryButton?.Click -= OnSecondaryButtonClick;
            _closeButton?.Click -= OnCloseButtonClick;
            base.OnApplyTemplate();
            _primaryButton = GetTemplateChild(PART_PrimaryButton) as ButtonBase;
            _secondaryButton = GetTemplateChild(PART_SecondaryButton) as ButtonBase;
            _closeButton = GetTemplateChild(PART_CloseButton) as ButtonBase;
            _primaryButton?.Click += OnPrimaryButtonClick;
            _secondaryButton?.Click += OnSecondaryButtonClick;
            _closeButton?.Click += OnCloseButtonClick;
        }

        /// <inheritdoc />
        protected override void OnPreviewKeyDown(KeyEventArgs e)
        {
            base.OnPreviewKeyDown(e);
            if (e.Handled || _showCompletionSource is null)
            {
                return;
            }

            if (e.Key == Key.Escape)
            {
                HandleButtonInvoked(CloseButtonClick, CloseButtonCommand, CloseButtonCommandParameter, ContentDialogResult.None);
                e.Handled = true;
                return;
            }

            if (e.Key == Key.Enter)
            {
                IInputElement? focused = Keyboard.FocusedElement;
                if (ReferenceEquals(focused, _primaryButton)
                    || ReferenceEquals(focused, _secondaryButton)
                    || ReferenceEquals(focused, _closeButton))
                {
                    // Let the focused command button handle Enter as a native click.
                    return;
                }

                if (TryInvokeDefaultButton())
                {
                    e.Handled = true;
                }
            }
        }

        /// <summary>
        /// Swallows tunneling mouse presses anywhere in the owner window that do not originate
        /// inside the dialog, so chrome outside the content adorner (such as a title-bar search
        /// box) cannot be clicked or focused while the dialog is modal.
        /// </summary>
        private void OnOwnerPreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (_showCompletionSource is null)
            {
                return;
            }

            if (e.OriginalSource is DependencyObject source && IsWithinDialog(source))
            {
                return;
            }

            e.Handled = true;
        }

        /// <summary>
        /// Walks the visual (and, for non-visual sources, logical) tree from
        /// <paramref name="source"/> upward to determine whether it sits inside this dialog.
        /// </summary>
        private bool IsWithinDialog(DependencyObject source)
        {
            DependencyObject? current = source;
            while (current is not null)
            {
                if (ReferenceEquals(current, this))
                {
                    return true;
                }

                current = current is Visual or System.Windows.Media.Media3D.Visual3D
                    ? VisualTreeHelper.GetParent(current)
                    : LogicalTreeHelper.GetParent(current);
            }

            return false;
        }

        private static object CoerceButtonText(DependencyObject d, object? baseValue)
        {
            return baseValue ?? string.Empty;
        }

        /// <summary>
        /// Resolves the window that hosts the modal overlay: the active window first, then
        /// the application main window.
        /// </summary>
        private static Window ResolveOwnerWindow()
        {
            Application application = Application.Current
                ?? throw new InvalidOperationException(
                    "ContentDialog.ShowAsync requires a running Application to resolve the owner window.");

            Window? active = null;
            foreach (Window window in application.Windows)
            {
                if (window.IsActive)
                {
                    active = window;
                    break;
                }
            }

            return active
                ?? application.MainWindow
                ?? throw new InvalidOperationException(
                    "ContentDialog.ShowAsync could not resolve an owner window. Show a window before opening the dialog.");
        }

        private void OnPrimaryButtonClick(object sender, RoutedEventArgs e)
        {
            HandleButtonInvoked(PrimaryButtonClick, PrimaryButtonCommand, PrimaryButtonCommandParameter, ContentDialogResult.Primary);
        }

        private void OnSecondaryButtonClick(object sender, RoutedEventArgs e)
        {
            HandleButtonInvoked(SecondaryButtonClick, SecondaryButtonCommand, SecondaryButtonCommandParameter, ContentDialogResult.Secondary);
        }

        private void OnCloseButtonClick(object sender, RoutedEventArgs e)
        {
            HandleButtonInvoked(CloseButtonClick, CloseButtonCommand, CloseButtonCommandParameter, ContentDialogResult.None);
        }

        /// <summary>
        /// Runs the shared command-button pipeline: raises the button's click event, and when
        /// not canceled executes the button's command and closes the dialog with the given result.
        /// </summary>
        private void HandleButtonInvoked(
            EventHandler<ContentDialogButtonClickEventArgs>? clickHandler,
            ICommand? command,
            object? commandParameter,
            ContentDialogResult result)
        {
            ContentDialogButtonClickEventArgs args = new();
            clickHandler?.Invoke(this, args);
            if (args.Cancel)
            {
                return;
            }

            if (command is not null && command.CanExecute(commandParameter))
            {
                command.Execute(commandParameter);
            }

            CloseDialog(result);
        }

        /// <summary>
        /// Invokes the pipeline of the button selected by <see cref="DefaultButton"/> when
        /// that button is currently visible and enabled.
        /// </summary>
        /// <returns><see langword="true"/> when a default button was invoked.</returns>
        private bool TryInvokeDefaultButton()
        {
            if (DefaultButton == ContentDialogButton.Primary && IsPrimaryButtonEnabled && !string.IsNullOrWhiteSpace(PrimaryButtonText))
            {
                HandleButtonInvoked(PrimaryButtonClick, PrimaryButtonCommand, PrimaryButtonCommandParameter, ContentDialogResult.Primary);
                return true;
            }

            if (DefaultButton == ContentDialogButton.Secondary && IsSecondaryButtonEnabled && !string.IsNullOrWhiteSpace(SecondaryButtonText))
            {
                HandleButtonInvoked(SecondaryButtonClick, SecondaryButtonCommand, SecondaryButtonCommandParameter, ContentDialogResult.Secondary);
                return true;
            }

            if (DefaultButton == ContentDialogButton.Close && !string.IsNullOrWhiteSpace(CloseButtonText))
            {
                HandleButtonInvoked(CloseButtonClick, CloseButtonCommand, CloseButtonCommandParameter, ContentDialogResult.None);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Moves keyboard focus to the <see cref="DefaultButton"/> when one is available,
        /// otherwise to the dialog itself, so the Tab cycle starts inside the dialog.
        /// </summary>
        private void MoveInitialFocus()
        {
            if (_showCompletionSource is null)
            {
                // The dialog was closed before the queued focus callback ran.
                return;
            }

            ButtonBase? defaultButton = DefaultButton switch
            {
                ContentDialogButton.Primary => _primaryButton,
                ContentDialogButton.Secondary => _secondaryButton,
                ContentDialogButton.Close => _closeButton,
                ContentDialogButton.None or _ => null,
            };

            if (defaultButton is not null && defaultButton.IsEnabled && defaultButton.Visibility == Visibility.Visible)
            {
                _ = defaultButton.Focus();
                return;
            }

            _ = Focus();
        }

        /// <summary>
        /// Removes the overlay adorner, restores the previously focused element, completes
        /// the pending <see cref="ShowAsync"/> task with <paramref name="result"/>, and
        /// raises <see cref="Closed"/>.
        /// </summary>
        private void CloseDialog(ContentDialogResult result)
        {
            if (_showCompletionSource is null)
            {
                return;
            }

            TaskCompletionSource<ContentDialogResult> completionSource = _showCompletionSource;
            _showCompletionSource = null;

            if (_owner is not null && _ownerInputBlocker is not null)
            {
                _owner.RemoveHandler(PreviewMouseDownEvent, _ownerInputBlocker);
            }

            _owner = null;
            _ownerInputBlocker = null;

            _overlayRoot?.Children.Remove(this);

            _overlayHostPanel?.Children.Remove(_overlayRoot);
            _overlayHostPanel = null;

            if (_hostAdorner is not null)
            {
                _hostLayer?.Remove(_hostAdorner);
                _hostAdorner = null;
            }

            _hostLayer = null;
            _overlayRoot = null;

            if (_previousFocus is not null)
            {
                _ = Keyboard.Focus(_previousFocus);
                _previousFocus = null;
            }

            _ = completionSource.TrySetResult(result);
            Closed?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Builds the overlay layout root: a smoke layer that dims and blocks everything behind
        /// it, with the dialog centered on top and Tab navigation trapped inside.
        /// </summary>
        private Grid BuildOverlayRoot()
        {
            System.Windows.Controls.Border smoke = new();
            smoke.SetResourceReference(System.Windows.Controls.Border.BackgroundProperty, "SmokeFillColorDefaultBrush");

            Grid root = new() { Focusable = false };
            KeyboardNavigation.SetTabNavigation(root, KeyboardNavigationMode.Cycle);
            KeyboardNavigation.SetControlTabNavigation(root, KeyboardNavigationMode.Cycle);
            KeyboardNavigation.SetDirectionalNavigation(root, KeyboardNavigationMode.Contained);
            _ = root.Children.Add(smoke);
            _ = root.Children.Add(this);
            return root;
        }

        /// <summary>
        /// The primary command button wired from the template, when present.
        /// </summary>
        private ButtonBase? _primaryButton;

        /// <summary>
        /// The secondary command button wired from the template, when present.
        /// </summary>
        private ButtonBase? _secondaryButton;

        /// <summary>
        /// The close command button wired from the template, when present.
        /// </summary>
        private ButtonBase? _closeButton;

        /// <summary>
        /// Completion source for the pending <see cref="ShowAsync"/> call; non-null while the
        /// dialog is open.
        /// </summary>
        private TaskCompletionSource<ContentDialogResult>? _showCompletionSource;

        /// <summary>
        /// The adorner hosting the smoke layer and the dialog while open.
        /// </summary>
        private DialogHostAdorner? _hostAdorner;

        /// <summary>
        /// The adorner layer the overlay was added to while open.
        /// </summary>
        private AdornerLayer? _hostLayer;

        /// <summary>
        /// The element that had keyboard focus before the dialog opened; restored on close.
        /// </summary>
        private IInputElement? _previousFocus;

        /// <summary>
        /// The owner window whose pointer input is blocked while the dialog is open.
        /// </summary>
        private Window? _owner;

        /// <summary>
        /// The tunneling preview-mouse handler attached to <see cref="_owner"/> that swallows
        /// presses outside the dialog while it is open; retained so it can be removed on close.
        /// </summary>
        private MouseButtonEventHandler? _ownerInputBlocker;

        /// <summary>
        /// The overlay layout root (smoke layer plus the dialog) added to the host while open.
        /// </summary>
        private Grid? _overlayRoot;

        /// <summary>
        /// The full-window overlay host panel (a window template's <c>PART_DialogOverlayHost</c>)
        /// when the dialog is hosted above the whole window rather than in the content adorner layer.
        /// </summary>
        private System.Windows.Controls.Panel? _overlayHostPanel;

        /// <summary>
        /// Hosts the smoke layer and the centered dialog inside the owner window's adorner
        /// layer. The smoke Border paints <c>SmokeFillColorDefaultBrush</c> across the full
        /// adorned bounds and stays hit-test visible so it blocks mouse input to the window
        /// content beneath; Tab navigation is trapped inside the layout root.
        /// </summary>
        private sealed class DialogHostAdorner : Adorner
        {
            public DialogHostAdorner(UIElement adornedElement, Grid layoutRoot)
                : base(adornedElement)
            {
                _layoutRoot = layoutRoot;
                _visuals = new VisualCollection(this);
                _ = _visuals.Add(_layoutRoot);
            }

            /// <inheritdoc />
            protected override int VisualChildrenCount => _visuals.Count;

            /// <inheritdoc />
            protected override Visual GetVisualChild(int index)
            {
                return _visuals[index];
            }

            /// <inheritdoc />
            protected override Size MeasureOverride(Size constraint)
            {
                Size bounds = AdornedElement.RenderSize;
                _layoutRoot.Measure(bounds);
                return bounds;
            }

            /// <inheritdoc />
            protected override Size ArrangeOverride(Size finalSize)
            {
                Size bounds = AdornedElement.RenderSize;
                _layoutRoot.Arrange(new Rect(bounds));
                return bounds;
            }

            /// <summary>
            /// The single visual child (the overlay layout root) exposed to the visual tree.
            /// </summary>
            private readonly VisualCollection _visuals;

            /// <summary>
            /// The overlay layout root (smoke layer below the centered dialog).
            /// </summary>
            private readonly Grid _layoutRoot;
        }
    }
}
