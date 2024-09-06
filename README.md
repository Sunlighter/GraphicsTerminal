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

The terminal has only a few simple functions.

### GetEventAsync

`GetEventAsync` draws graphics, blocks until a desired event occurs, and then returns the event, which is an object
inheriting from the `TerminalEvent` class. `GetEventAsync` creates a `Bitmap` and draws the graphics onto the
bitmap. It takes three arguments: a `System.Drawing.Size` indicating the desired size of the bitmap, an
`Action<System.Drawing.Graphics>` function, which is expected to draw the graphics on the bitmap, and a set of
`EventFlags` which indicates what events you want.

Be sure to call <code>Clear</code> first, and to use colors with opaque alpha values.

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

Note that a `TE_UserCloseRequest` event can also be returned by any call to `GetEventAsync`. This indicates that the
user clicked the box to close the window. This event can happen at any time, and there is no way to specify that you
don&rsquo;t want it.

### GetBigTextAsync

`GetBigTextAsync` replaces the entire graphics window with a multi-line text edit control. You have to specify initial
text, which can be an empty string. You can allow the user to edit this text or keep it read-only for display purposes
(the user will still be able to select the text and copy it to the clipboard). You can also display your choice of
`MessageBoxButtons`.

The function takes four arguments: `labelText` (which appears above the edit control and may be something like
&ldquo;Type your text here:&rdquo;), `isReadOnly`, `content`, and `buttons`.

You will get back a `TE_BigTextEntry` object, which contains a `DialogResult` and a `Text` property.  The modified
text is always returned, even if the user clicked `Cancel`, so be careful to honor the user&rsquo;s wishes and discard
it when appropriate.

It is also possible you will get back a `TE_UserCloseRequest`.

### ShowBusyFormAsync

The terminal window doesn&rsquo;t use the hourglass icon, because the assumption is that the controlling thread will
spend most of its time waiting for `GetEventAsync` or `GetBigTextAsync`, and flickering is undesirable.

If your main thread is going to do something that keeps it busy for a while, you might want to put up a busy form, so
that the user knows what to expect.

`ShowBusyFormAsync` takes a `busyDoing` string describing what the program is busy doing (e.g.,
&ldquo;Working...&rdquo;), an optional `progressAmount` (which is shown in the progress bar, or if unspecified, the
progress bar is put in &ldquo;Marquee&rdquo; mode), and an optional `CancellationTokenSource` called `cts`.

If you provide the `CancellationTokenSource`, a `Cancel` button will be displayed, and if the user clicks the button,
it will be disabled (to indicate to the user that the cancellation is now in progress) and the cancellation token will
be cancelled.

If you do not provide a `CancellationTokenSource`, the `Cancel` button is not displayed.

The `ShowBusyFormAsync` function is `async` but returns &ldquo;immediately,&rdquo; as soon as the terminal has started
displaying the busy status.

You can call `ShowBusyFormAsync` again to update progress.

To get rid of the busy form, call any of the other terminal functions.

### ShowDialogAsync

This function allows you to show an arbitrary modal dialog; you pass a function and it passes your function a
`IWin32Window` object to serve as a parent for the dialog. This way you can use common dialogs such as
`OpenFileDialog`, or you can use `MessageBox.Show`, or you can use your own dialogs.

Your return type should include an indication of whether the user entered data or canceled the dialog.

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
