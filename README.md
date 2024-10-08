<!-- -*- coding: utf-8; fill-column: 118 -*- -->

# GraphicsTerminalLib
Implementation of a Graphics Terminal

## Purpose and Features

This library gives you a &ldquo;graphics terminal&rdquo; which can draw graphics and request user input.

It works on the assumption that your program can block while waiting for user input. This is consistent with the
&ldquo;old paradigm&rdquo; used by 80s microcomputers and DOS programs and (today) console programs. Some programs are
easier to write this way. (The &ldquo;new paradigm,&rdquo; which is still very old, is to write an &ldquo;event
handler&rdquo; which has to be able to handle any event at any time. The advantage of the &ldquo;new paradigm&rdquo;
is that it&rsquo;s easy to forward events to any of several recipients, like having multiple windows open at the same
time.)

This library does not emulate a text terminal. It lets you draw graphics onto a bitmap and it scales the bitmap to fit
in its window (regardless of aspect ratio). This is also easier to program, but may cause a blocky (or
&ldquo;retro&rdquo;) appearance due to the bitmap scaling.

Using a bitmap in this way is somewhat inefficient, because the entire bitmap is replaced and redrawn every time an
event is requested. This is fine for some programs because they would do something like that anyway, but it would not
be very efficient if you wanted to write a text terminal emulator, because it would end up having to redraw the whole
screen just to turn the cursor on and off.

For events, the library supports mouse clicks and keypresses, and it has a couple of text input facilities, but it
does not support detecting mouse motion, or drag-and-drop.

This library should be sufficient for writing simple graphics editors, font editors, and the like. These kinds of
programs cannot be written on a text console, because they need graphics, but they might be easier to write if they
don&rsquo;t have to address the &ldquo;new paradigm.&rdquo; (One way to handle the &ldquo;new paradigm&rdquo; is to
use state machines or coroutines or alternate threads. This library itself uses an alternate thread.)

I am releasing this program as open source so that anyone can use it as a base and make modifications, such as adding
features or remedying any deficiencies. I may also add features later, depending on how I try to use it.

## Quick Start Guide

First, use Visual Studio to create a console program. Right now, dot-Net 8 is supported.

Then, edit the `.csproj` file and change the `TargetFramework` from `net8.0` to `net8.0-windows` to enable support for
Windows Forms.

If you don't actually want to use the Windows Console, change the `OutputType` from `Exe` to `WinExe`.

You can then add the `Sunlighter.GraphicsTerminalLib` NuGet package.

Make sure your `Main` function is `async`.

In the `Main` function, you can construct a `Sunlighter.GraphicsTerminalLib.GraphicsTerminal` object with the `new`
operator. (Usually you would use `await using` for this object.) It runs as its own window in its own thread. The
only constructor argument is the title for the window.

You would not generally want to create two of these objects (because your task can only wait for an event from one of
them at a time), but you could possibly create multiple tasks, each of which has its own terminal object. These
terminal objects would appear as unrelated windows on the screen.

## Functions

The terminal has only a few simple functions.

### GetEventAsync

`GetEventAsync` displays graphics, blocks until a desired event occurs, and then returns the event, which is an object
inheriting from the `TerminalEvent` class. There are multiple overloads depending on where to get the graphics from.

`GetEventAsync(Size actionSize, Action<Graphics> draw, EventFlags flags)` takes an `actionSize`, creates a new bitmap
of that size, creates a `Graphics` to draw on that bitmap, and passes the `Graphics` to your `draw` function. The
bitmap will be scaled to fill the drawing area. Be sure to call <code>Clear</code> first, and to use colors with
opaque alpha values.

`GetEventAsync(Bitmap newBitmap, Option<DisposableBox<Bitmap>> bitmapReturn, EventFlags flags)` takes an
already-created `Bitmap` and uses that. If the `DisposableBox<Bitmap>` is provided, it will receive the old bitmap, if
there was one; otherwise, the old bitmap will be disposed. The new bitmap will be scaled to fit the drawing area.

`GetEventAsync(Func<Size, Bitmap> createBitmap, Option<DisposableBox<Bitmap>> bitmapReturn, EventFlags flags)` gets
the current size of the drawing area and passes it to `createBitmap`, which is expected to create a new bitmap and
return it. The new bitmap is not required to be of the provided size, and will be scaled to fit the drawing area if
necessary.

`GetEventAsync(Func<Bitmap?, Size, Bitmap> createBitmap, EventFlags flags)` gets the old bitmap (or `null` if there
wasn&rsquo;t a previous bitmap), and passes it to `createBitmap`, which may either modify the given bitmap and return
it, or create a new bitmap and arrange for the old one to be disposed eventually. The size of the drawing area is
provided and can be compared against the size of the bitmap.

Any `createBitmap` or `draw` function will be called exactly once.

All the `GetEventAsync` overloads take a `flags` argument which indicates the events you want.

The `EventFlags` are:

* `TimerTick`, which corresponds to the `TE_TimerTick` event class. This event is caused by a timer which is currently
hard-coded to tick every 250 ms, and is started when the window is created. This timer can be used to drive simple
animations.

* `MouseClick`, which corresponds to the `TE_MouseClick` event class. This event is caused by the user clicking in the
drawing area of the window. The bitmap coordinates of the click are included.

* `TextEntry`, which corresponds to the `TE_TextEntry` event class. If this flag is specified, a small text input area
appears at the bottom of the window, with a &ldquo;Submit&rdquo; button, and the user can type text there and submit
it, causing this event. If this flag is *not* specified, the text entry area does not appear.

* `NewTextEntry` indicates that the text entry area should be cleared; this flag is ignored unless the `TextEntry`
flag is also specified. This flag exists because `GetEventAsync` can look for more than one event; for example, a
timer tick could occur while the user is still typing, and if the `TimerTick` flag was also passed, the event will
cause the `TE_TimerTick` event to be returned. In this case, the user is still typing, and so you probably want to
update the graphics and call `GetEventAsync` again, with the `TextEntry` flag set but *not* the `NewTextEntry` flag,
so that the user can continue typing.

* `KeyDown`, which corresponds to the `TE_KeyDown` event class. This event is caused by the user pressing a key in the
drawing (*not* in the text entry area, which can be focused separately, or might be hidden).  The
`System.Windows.Forms.Keys` value is included in the event. (The code takes extra steps to ensure that arrow keys are
treated like any other keys.)

* `SizeChanged`, which corresponds to the `TE_SizeChanged` event class. This event is caused by the window being
resized and indicates the new size of the drawing area.

Note that a `TE_UserCloseRequest` event can also be returned by any call to `GetEventAsync`. This indicates that the
user clicked the box to close the window. This event can happen at any time, and there is no way to specify that you
don&rsquo;t want it.

### GetBigTextAsync

`GetBigTextAsync` replaces the entire graphics window with a multi-line text edit control. You have to specify initial
text, which can be an empty string. You can allow the user to edit this text or keep it read-only for display purposes
(the user will still be able to select the text and copy it to the clipboard). You can also display your choice of
`MessageBoxButtons`.

There are two overloads to this function. The first one takes four arguments: `labelText` (which appears above the
edit control and may be something like &ldquo;Type your text here:&rdquo;), `isReadOnly`, `content`, and
`buttons`. The second overload takes a `contentReturn` argument, which is a `StrongBox<string>`. (The `contentReturn`
argument comes after the `content` argument in the list.)

You will typically get back a `TE_BigTextEntry` object, which contains a `DialogResult` and a `Text` property.  The
modified text is always returned, even if the user clicked `Cancel`, so be careful to honor the user&rsquo;s wishes
and discard it when appropriate.

It is also possible you will get back a `TE_UserCloseRequest`.

If the `contentReturn` argument was specified, the value of the `StrongBox<string>` will be set to the string that was
in the box at the time `GetBigTextAsync` returned (regardless of whether it returned `TE_BigTextEntry` or
`TE_UserCloseRequest`). This allows that, if the close box was clicked, you can ask the user if they&rsquo;re sure,
and if they choose &ldquo;no,&rdquo; you can return them to the text editor with the same text as before, so they
don&rsquo;t lose their changes.

### ShowBusyFormAsync

The terminal window doesn&rsquo;t use the hourglass icon, because the assumption is that the controlling thread will
spend most of its time waiting for `GetEventAsync` or `GetBigTextAsync`, and flickering is undesirable.

If your main thread is going to do something that keeps it busy for a while, you might want to put up a busy form, so
that the user knows what to expect.

`ShowBusyFormAsync` takes a `busyDoing` string describing what the program is busy doing (e.g.,
&ldquo;Working...&rdquo;), an optional `progressAmount` (which is shown in the progress bar, or if unspecified, the
progress bar is put in &ldquo;Marquee&rdquo; mode), and an optional `CancellationTokenSource` called `cts`.

If you provide the `CancellationTokenSource`, a `Cancel` button will be displayed, and if the user clicks the button,
the button will be disabled (to indicate to the user that the cancellation is now in progress) and the cancellation
token will be cancelled. If the user tries to close the window, it will simulate clicking the `Cancel` button.

If you do not provide a `CancellationTokenSource`, the `Cancel` button is not displayed.

If the user tries to close the window, and there is no `Cancel` button, then the window enters a &ldquo;pending
close&rdquo; state. There is no visible indication of this state. However, if the window is in this state, the next
call to `GetEventAsync` or `GetBigTextAsync` will return `TE_UserCloseRequest` (and clear the &ldquo;pending
close&rdquo; state), and the next call to `ShowBusyFormAsync` which is cancellable will simulate clicking the `Cancel`
button immediately (and will clear the &ldquo;pending close&rdquo; state). Note that `ShowDialogAsync` cannot detect
the &ldquo;pending close&rdquo; state and does not affect it.

The `ShowBusyFormAsync` function is `async` but returns &ldquo;immediately,&rdquo; as soon as the terminal has started
displaying the busy status.

You can call `ShowBusyFormAsync` again to update progress.

To get rid of the busy form, call any of the other terminal functions.

### ShowDialogAsync

This function allows you to show an arbitrary modal dialog; you pass a function and it passes your function a
`IWin32Window` object to serve as a parent for the dialog. This way you can use common dialogs such as
`OpenFileDialog`, or you can use `MessageBox.Show`, or you can use your own dialogs.

Your return type should include an indication of whether the user entered data or canceled the dialog.

### CheckPendingCloseAsync

This function checks for the &ldquo;pending close&rdquo; state and returns &ldquo;immediately.&rdquo; The
&ldquo;pending close&rdquo; state occurs when the user clicks the window&rsquo;s Close box, but the thread using the
terminal is busy and not waiting for an event. If there is a pending &ldquo;close,&rdquo; this function clears it and
returns a `TE_UserCloseRequest` object, otherwise it returns a `TE_Nothing` object.

The `TE_Nothing` object is not returned by any other function than this one.

Note that a pending close is also cleared if `GetEventAsync` or `GetBigTextAsync` is called (in which case it
immediately returns `TE_UserCloseRequest`), or if a cancellable busy screen is displayed (in which case an immediate
click of the Cancel button is simulated).

If the controlling thread has been busy, it&rsquo;s a good idea to call this function before calling
`ShowDialogAsync`, which does not check for a pending close.

### DisposeAsync

Usually you wouldn&rsquo;t call this directly, you would set up `await using` instead, but you should know that it
causes the terminal window to immediately close. Usually you should arrange for this to happen if you received the
`TE_UserCloseRequest` event, although you have the option to ask the user to save their data first, if appropriate.

## Warnings

Behavior is undefined if two or more threads or tasks try to use the same graphics terminal at the same time. However,
it is safe to pass a graphics terminal from one thread or task to another.

**Breaking Change.** In version 1.0.0, the functions `ShowBusyFormAsync` and `ShowDialogAsync` were incorrectly named
`ShowBusyForm` and `ShowDialog`. The functionality was the same. I detected this misnaming after a few hours, and even
though a renaming like this is a breaking change, I released the fix as version 1.0.1 (instead of 2.0.0 as Semantic
Versioning would have required), because 1.0.0 was only out there for a few hours. I recommend not using 1.0.0.
