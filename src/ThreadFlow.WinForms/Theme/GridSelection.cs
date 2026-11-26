namespace ThreadFlow.WinForms.Theme;

/// <summary>
/// Shared table behaviour for every data table in the app: no row is highlighted on its own when
/// a table is populated or shown, and a click anywhere off a table drops the row it had selected.
/// </summary>
internal static class GridSelection
{
    /// <summary>Tables taking part in click-away clearing.</summary>
    private static readonly List<DataGridView> Tables = [];

    /// <summary>Tables where a row was chosen on purpose (a click, a key, or a restored selection).</summary>
    private static readonly HashSet<DataGridView> Chosen = [];

    private static ClickAwayFilter? _filter;

    /// <summary>
    /// Teaches one table the shared behaviour. Safe to call twice for the same table.
    /// </summary>
    internal static void Enable(DataGridView table)
    {
        ArgumentNullException.ThrowIfNull(table);

        if (Tables.Contains(table))
            return;

        Tables.Add(table);
        table.Disposed += (_, _) =>
        {
            Tables.Remove(table);
            Chosen.Remove(table);
        };

        // A DataGridView picks its first row by itself once it is bound. Remember whether the
        // person actually chose a row, so only that automatic pick is cleared.
        table.MouseDown += (_, e) =>
        {
            if (table.HitTest(e.X, e.Y).RowIndex >= 0)
                Chosen.Add(table);
        };
        table.KeyDown += (_, _) => Chosen.Add(table);

        table.DataBindingComplete += (_, _) => ClearIfUnchosen(table);
        table.HandleCreated += (_, _) => ClearIfUnchosenLater(table);
        table.VisibleChanged += (_, _) =>
        {
            if (table.Visible)
                ClearIfUnchosenLater(table);
        };

        if (_filter is null)
        {
            _filter = new ClickAwayFilter();
            Application.AddMessageFilter(_filter);
        }
    }

    /// <summary>
    /// Marks the highlighted row as deliberate, so a later refresh does not clear it. Used when
    /// a screen re-selects the row the person just saved.
    /// </summary>
    internal static void MarkSelected(DataGridView table) => Chosen.Add(table);

    private static void ClearIfUnchosen(DataGridView table)
    {
        if (!Chosen.Contains(table))
            Clear(table);
    }

    private static void ClearIfUnchosenLater(DataGridView table)
    {
        if (Chosen.Contains(table))
            return;

        // The automatic pick happens after the table is shown and laid out, so clear it once the
        // current message has finished processing.
        ClearLater(table);
    }

    private static void Clear(DataGridView table)
    {
        if (table.IsDisposed)
            return;

        if (table.IsCurrentCellInEditMode)
            table.EndEdit();

        table.ClearSelection();
        table.CurrentCell = null;
    }

    private static void ClearLater(DataGridView table)
    {
        if (!table.IsHandleCreated || table.IsDisposed)
            return;

        try
        {
            table.BeginInvoke(new Action(() =>
            {
                if (!table.IsDisposed)
                    Clear(table);
            }));
        }
        catch (InvalidOperationException)
        {
            // The table lost its window between the check and the post; nothing left to clear.
        }
    }

    /// <summary>
    /// Watches the application's mouse buttons and drops a table's selection when the click lands
    /// somewhere else on the page. It runs on release, after the clicked control's own handler,
    /// so buttons that act on the selected row still see it.
    /// </summary>
    private sealed class ClickAwayFilter : IMessageFilter
    {
        private const int WmLeftButtonUp = 0x0202;
        private const int WmRightButtonUp = 0x0205;

        public bool PreFilterMessage(ref Message m)
        {
            if (m.Msg is not (WmLeftButtonUp or WmRightButtonUp))
                return false;

            ClearTablesAwayFromClick(m.HWnd);

            // The click is never swallowed; only the selection is dropped.
            return false;
        }

        private static void ClearTablesAwayFromClick(IntPtr clickedWindow)
        {
            if (Tables.Count == 0)
                return;

            // Only the window that was clicked matters: a dialog's table must not be cleared from
            // a click on the page behind it, or the other way round.
            var page = Control.FromHandle(clickedWindow)?.FindForm() ?? Form.ActiveForm;
            if (page is null)
                return;

            var pointer = Cursor.Position;

            for (var index = Tables.Count - 1; index >= 0; index--)
            {
                var table = Tables[index];

                if (table.IsDisposed)
                {
                    Tables.RemoveAt(index);
                    continue;
                }

                if (!table.Visible || !table.IsHandleCreated)
                    continue;

                if (!ReferenceEquals(page, table.FindForm()))
                    continue;

                if (table.ClientRectangle.Contains(table.PointToClient(pointer)))
                    continue;

                if (table.SelectedRows.Count == 0 && table.CurrentCell is null)
                    continue;

                // Deferred: the button that was clicked reads the selected row in its Click
                // handler, which runs after this message.
                ClearLater(table);

                // Nothing is chosen any more, so the next refresh starts unselected again.
                Chosen.Remove(table);
            }
        }
    }
}
