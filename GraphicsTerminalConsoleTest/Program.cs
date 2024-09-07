using Sunlighter.GraphicsTerminalLib;
using Sunlighter.OptionLib;
using System.Collections.Immutable;
using System.Drawing;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Windows.Forms;

namespace GraphicsTerminalConsoleTest
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            await using GraphicsTerminal terminal = new GraphicsTerminal("Graphics Terminal Test");

            Random r = new Random();

            bool haveMouseClick = false;
            int mcx = 0;
            int mcy = 0;

            ImmutableSortedDictionary<Keys, string> keyNames = ImmutableSortedDictionary<Keys, string>.Empty;

            foreach (FieldInfo fi in typeof(Keys).GetFields(BindingFlags.Public | BindingFlags.Static))
            {
                if (fi.GetValue(null) is Keys k)
                {
                    if ((k & Keys.KeyCode) == k && k != Keys.KeyCode)
                    {
                        if (keyNames.TryGetValue(k, out string? names))
                        {
                            keyNames = keyNames.SetItem(k, names + ", " + fi.Name);
                        }
                        else
                        {
                            keyNames = keyNames.Add(k, fi.Name);
                        }
                    }
                }
            }

            await TestCancellation(terminal);

            // note: if the user clicks the Close box during the non-cancellable part,
            // the Close operation is queued, and passed to the next GetEvent call.
            // This causes TestTextEntryWithAnimation to eat the TE_UserCloseRequest
            // and return without doing anything.

            // This is a bug in this test program, not in the library.

            await TestTextEntryWithAnimation(terminal);

            while (true)
            {
                int xd1 = r.Next(30);
                int yd1 = r.Next(30);
                int xd2 = r.Next(30);
                int yd2 = r.Next(30);

                TerminalEvent te = await terminal.GetEventAsync
                (
                    new Size(512, 384),
                    g =>
                    {
                        g.Clear(Color.White);

                        using Brush b = new SolidBrush(Color.Black);
                        using Pen p = new Pen(b, 1.0f);

                        g.DrawLine(p, new Point(xd1, yd1), new Point(511 - xd2, 383 - yd2));

                        if (haveMouseClick)
                        {
                            g.DrawRectangle(p, new Rectangle(mcx - 2, mcy - 2, 4, 4));
                        }
                    },
                    EventFlags.MouseClick | EventFlags.KeyDown
                );

                if (te is TE_MouseClick mc)
                {
                    haveMouseClick = true;
                    mcx = (int)mc.X;
                    mcy = (int)mc.Y;
                }
                else if (te is TE_KeyDown kd)
                {
                    haveMouseClick = false;
                    Keys kc = kd.KeyData & Keys.KeyCode;
                    if (keyNames.TryGetValue(kc, out string? names))
                    {
                        Console.WriteLine($"Key: {names}");
                    }
                    else
                    {
                        Console.WriteLine($"Key: {(int)kc}");
                    }

                    if (kc == Keys.A)
                    {
                        StrongBox<string> textReturn = new StrongBox<string>();
                        bool returning = false;

                        redoText:

                        te = await terminal.GetBigTextAsync
                        (
                            "Example:",
                            false,
                            returning ? (textReturn.Value ?? string.Empty) : DateTime.Now.ToString("G"),
                            textReturn,
                            MessageBoxButtons.OKCancel
                        );

                        if (te is TE_BigTextEntry bte)
                        {
                            Console.WriteLine($"DialogResult: {bte.DialogResult}");
                        }
                        else if (te is TE_UserCloseRequest)
                        {
                            TerminalEvent te2 = await terminal.GetEventAsync
                            (
                                new Size(400, 300),
                                g =>
                                {
                                    g.Clear(Color.White);
                                    using Brush br = new SolidBrush(Color.Blue);
                                    using Font f = new Font("Lucida Console", 12.0f, FontStyle.Regular, GraphicsUnit.Point);

                                    string prompt = "Are you sure? [Y/N]";
                                    SizeF f0 = g.MeasureString(prompt, f);
                                    float xpos = (400.0f - f0.Width) / 2.0f;
                                    float ypos = (300.0f - f0.Height) / 2.0f;
                                    g.DrawString(prompt, f, br, new PointF(xpos, ypos));
                                },
                                EventFlags.KeyDown
                            );

                            if (te2 is TE_KeyDown kd2)
                            {
                                if ((kd2.KeyData & Keys.KeyCode) == Keys.N)
                                {
                                    returning = true;
                                    goto redoText;
                                }
                            }

                            te = TE_UserCloseRequest.Value;
                        }
                    }
                    else if (kc == Keys.B)
                    {
                        DialogResult dr = await terminal.ShowDialogAsync<DialogResult>
                        (
                            parent =>
                            {
                                return MessageBox.Show(parent, "Hello!", "Message Box Test", MessageBoxButtons.OK, MessageBoxIcon.Information);
                            }
                        );
                    }
                }

                if (te is TE_UserCloseRequest) break;
            }

            await terminal.ShowDialogAsync
            (
                parent =>
                {
                    return MessageBox.Show
                    (
                        "Text",
                        "Caption",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information
                    );
                }
            );

            await terminal.GetEventAsync
            (
                new Size(512, 192),
                g =>
                {
                    g.Clear(Color.White);

                    using Brush b = new SolidBrush(Color.Blue);
                    using Font f = new Font("Times New Roman", 24.0f, FontStyle.Regular);

                    g.DrawString("Next action quits", f, b, new PointF(5.0f, 5.0f), StringFormat.GenericTypographic);
                },
                EventFlags.TextEntry
            );
        }

        private static async Task TestCancellation(GraphicsTerminal terminal)
        {
            using CancellationTokenSource cts = new();
            for (int i = 0; i < 20; ++i)
            {
                await terminal.ShowBusyFormAsync
                (
                    "Testing busy dialog...",
                    Option<double>.Some(i / 20.0),
                    Option<CancellationTokenSource>.Some(cts)
                );

                Thread.Sleep(2000);

                if (cts.IsCancellationRequested) break;
            }

            await terminal.ShowBusyFormAsync
            (
                "Not cancellable...",
                Option<double>.None,
                Option<CancellationTokenSource>.None
            );

            Thread.Sleep(3000);
        }

        private static async Task TestTextEntryWithAnimation(GraphicsTerminal terminal)
        {
            string? s1 = await GetTextWithAnimation(terminal);

            if (s1 is not null)
            {
                string? s2 = await GetTextWithAnimation(terminal);

                if (s2 is not null)
                {
                    await terminal.GetBigTextAsync
                    (
                        "Results",
                        true,
                        s1 + ", " + s2,
                        MessageBoxButtons.OK
                    );
                }
            }
        }

        private static async Task<string?> GetTextWithAnimation(GraphicsTerminal terminal)
        {
            int x = 0;
            bool first = true;
            while(true)
            {
                TerminalEvent te = await terminal.GetEventAsync
                (
                    new Size(512, 384),
                    g =>
                    {
                        g.Clear(Color.Black);

                        using Brush b = new SolidBrush(Color.FromArgb(0x80, 0x00, 0xFF));
                        using Pen p = new Pen(b);

                        g.DrawLine(p, x, 0, x, 383);
                    },
                    EventFlags.TextEntry | EventFlags.TimerTick | EventFlags.MouseClick | (first ? EventFlags.NewTextEntry : EventFlags.None)
                );

                first = false;

                if (te is TE_UserCloseRequest)
                {
                    return null;
                }
                else if (te is TE_TextEntry txe)
                {
                    return txe.Text;
                }
                else if (te is TE_MouseClick mc)
                {
                    x = (int)Math.Round(Math.Max(0.0, Math.Min(511.0, mc.X)));
                }
                else
                {
                    ++x;
                    if (x == 512) x = 0;
                }
            }
        }
    }
}
