<%@ Page %>

<cs:Content PlaceHolder="Head">
  <link rel="stylesheet" href="/css/pages/admin.css" />
</cs:Content>

<cs:Content PlaceHolder="Body">
  <div class="admin-page">
    <section class="admin-hero">
      <div>
        <p class="eyebrow">Backend Management</p>
        <h1>Library Admin</h1>
        <p>Manage books, readers, circulation, reservations, overdue loans, and fines from one MVVM page.</p>
      </div>
      <a class="screen-link" href="/">Back to Dashboard</a>
    </section>

    <cs:Region Name="admin-metrics" Tag="section" class="admin-metrics">
      <cs:Repeater Items="{Binding Metrics}">
        <article class="admin-metric metric-{Binding Tone}">
          <span>{Binding Label}</span>
          <strong>{Binding Value}</strong>
          <small>{Binding Hint}</small>
        </article>
      </cs:Repeater>
    </cs:Region>

    <cs:Form class="search-card">
      <cs:Field Name="Query" Label="Search" Placeholder="Title, author, or ISBN" />
      <cs:Field Name="FilterStatus" Label="Status" Placeholder="All, Available, Borrowed, Reserved, Overdue, Archived" />
      <cs:Field Name="FilterCategory" Label="Category" Placeholder="All or category name" />
      <cs:Button Command="Search">Apply Filter</cs:Button>
    </cs:Form>

    <cs:Region Name="admin-notices" Tag="section" class="notice-row">
      <cs:Repeater Items="{Binding Notices}">
        <div class="notice notice-{Binding Tone}">{Binding Text}</div>
      </cs:Repeater>
    </cs:Region>

    <section class="admin-grid admin-grid-wide">
      <form class="admin-card" method="post" data-cs-vm>
        <h2>Book Catalog</h2>
        <p>Add a book, or provide Book Id to update/archive/restore existing records.</p>
        <label>Book Id<input name="BookId" value="{Binding BookId}" data-cs-bind="BookId" /></label>
        <label>ISBN<input name="BookIsbn" value="{Binding BookIsbn}" data-cs-bind="BookIsbn" /></label>
        <label>Title<input name="BookTitle" value="{Binding BookTitle}" data-cs-bind="BookTitle" /></label>
        <label>Author<input name="BookAuthor" value="{Binding BookAuthor}" data-cs-bind="BookAuthor" /></label>
        <label>Category<input name="BookCategory" value="{Binding BookCategory}" data-cs-bind="BookCategory" /></label>
        <label>Location<input name="BookLocation" value="{Binding BookLocation}" data-cs-bind="BookLocation" /></label>
        <label>Year<input name="BookPublishedYear" value="{Binding BookPublishedYear}" data-cs-bind="BookPublishedYear" /></label>
        <label>Copies<input name="BookCopyCount" value="{Binding BookCopyCount}" data-cs-bind="BookCopyCount" /></label>
        <div class="button-row wrap">
          <button type="submit" data-cs-command="AddBook">Add</button>
          <button type="submit" data-cs-command="UpdateBook">Update</button>
          <button type="submit" data-cs-command="ArchiveBook">Archive</button>
          <button type="submit" data-cs-command="RestoreBook">Restore</button>
        </div>
      </form>

      <form class="admin-card" method="post" data-cs-vm>
        <h2>Readers</h2>
        <p>Register readers, update profiles, and control active/suspended status.</p>
        <label>Reader Id<input name="ReaderId" value="{Binding ReaderId}" data-cs-bind="ReaderId" /></label>
        <label>Name<input name="ReaderName" value="{Binding ReaderName}" data-cs-bind="ReaderName" /></label>
        <label>Email<input name="ReaderEmail" value="{Binding ReaderEmail}" data-cs-bind="ReaderEmail" /></label>
        <label>Phone<input name="ReaderPhone" value="{Binding ReaderPhone}" data-cs-bind="ReaderPhone" /></label>
        <label>Level<input name="ReaderLevel" value="{Binding ReaderLevel}" data-cs-bind="ReaderLevel" placeholder="Standard, Premium, Student" /></label>
        <div class="button-row wrap">
          <button type="submit" data-cs-command="RegisterReader">Register</button>
          <button type="submit" data-cs-command="UpdateReader">Update</button>
          <button type="submit" data-cs-command="SuspendReader">Suspend</button>
          <button type="submit" data-cs-command="ActivateReader">Activate</button>
        </div>
      </form>

      <form class="admin-card" method="post" data-cs-vm>
        <h2>Circulation</h2>
        <p>Borrow by Book Id and Reader Id. Return or renew by Loan Id.</p>
        <label>Book Id<input name="BookId" value="{Binding BookId}" data-cs-bind="BookId" /></label>
        <label>Reader Id<input name="LoanReaderId" value="{Binding LoanReaderId}" data-cs-bind="LoanReaderId" /></label>
        <label>Loan Id<input name="LoanId" value="{Binding LoanId}" data-cs-bind="LoanId" /></label>
        <div class="button-row wrap">
          <button type="submit" data-cs-command="BorrowBook">Borrow</button>
          <button type="submit" data-cs-command="ReturnBook">Return</button>
          <button type="submit" data-cs-command="RenewLoan">Renew</button>
        </div>
      </form>

      <form class="admin-card" method="post" data-cs-vm>
        <h2>Reservations and Fines</h2>
        <p>Queue reservations, cancel waiting reservations, and collect reader fines.</p>
        <label>Book Id<input name="BookId" value="{Binding BookId}" data-cs-bind="BookId" /></label>
        <label>Reader Id<input name="ReservationReaderId" value="{Binding ReservationReaderId}" data-cs-bind="ReservationReaderId" /></label>
        <label>Reservation Id<input name="ReservationId" value="{Binding ReservationId}" data-cs-bind="ReservationId" /></label>
        <label>Fine Reader Id<input name="ReaderId" value="{Binding ReaderId}" data-cs-bind="ReaderId" /></label>
        <label>Fine Amount<input name="FineAmount" value="{Binding FineAmount}" data-cs-bind="FineAmount" /></label>
        <div class="button-row wrap">
          <button type="submit" data-cs-command="ReserveBook">Reserve</button>
          <button type="submit" data-cs-command="CancelReservation">Cancel Reservation</button>
          <button type="submit" data-cs-command="CollectFine">Collect Fine</button>
        </div>
      </form>

      <article class="admin-card activity-card">
        <h2>Activity</h2>
        <cs:Repeater Items="{Binding Activities}">
          <div class="activity activity-{Binding Tone}">
            <time>{Binding Time}</time>
            <span>{Binding Text}</span>
          </div>
        </cs:Repeater>
      </article>

      <form class="admin-card inventory-card" method="post" data-cs-vm>
        <h2>Inventory Control</h2>
        <p>Receive new copies, write off damaged copies, and relocate collection items with audit history.</p>
        <label>Book Id<input name="InventoryBookId" value="{Binding InventoryBookId}" data-cs-bind="InventoryBookId" /></label>
        <label>Quantity<input name="InventoryQuantity" value="{Binding InventoryQuantity}" data-cs-bind="InventoryQuantity" /></label>
        <label>New Location<input name="InventoryLocation" value="{Binding InventoryLocation}" data-cs-bind="InventoryLocation" placeholder="A-01, B-12, Archive" /></label>
        <label>Reason<input name="InventoryReason" value="{Binding InventoryReason}" data-cs-bind="InventoryReason" placeholder="Purchase order, damaged copy, shelf optimization" /></label>
        <div class="button-row wrap">
          <button type="submit" data-cs-command="ReceiveCopies">Receive</button>
          <button type="submit" data-cs-command="WriteOffCopies">Write Off</button>
          <button type="submit" data-cs-command="RelocateBook">Relocate</button>
        </div>
      </form>

      <form class="admin-card import-export-card" method="post" data-cs-vm>
        <h2>CSV Import and Export</h2>
        <p>Export the current catalog filter, edit the CSV, and import it back. Existing ISBN values are updated, new ISBN values are added.</p>
        <label>CSV Workspace<textarea name="ImportExportCsv" data-cs-bind="ImportExportCsv" rows="9" placeholder="ISBN,Title,Author,Category,Location,PublishedYear,CopyCount,Rating">{Binding ImportExportCsv}</textarea></label>
        <div class="button-row wrap">
          <button type="submit" data-cs-command="ExportBooks">Export Filtered Books</button>
          <button type="submit" data-cs-command="ImportBooks">Import CSV</button>
          <button type="submit" data-cs-command="ClearImportExport">Clear</button>
        </div>
      </form>
    </section>

    <cs:Region Name="collection-table" Tag="section" class="book-table-card">
      <div class="panel-title">
        <h2>Collection</h2>
        <span>Filtered catalog with stock, reservation, and due-date context</span>
      </div>
      <div class="table-scroll">
        <cs:Table Items="{Binding Books}">
          <cs:Column Header="Id">{Binding Id}</cs:Column>
          <cs:Column Header="ISBN">{Binding Isbn}</cs:Column>
          <cs:Column Header="Title">{Binding Title}</cs:Column>
          <cs:Column Header="Author">{Binding Author}</cs:Column>
          <cs:Column Header="Category">{Binding Category}</cs:Column>
          <cs:Column Header="Status"><span class="status status-{Binding Status}">{Binding Status}</span></cs:Column>
          <cs:Column Header="Copies">{Binding AvailableCopies}/{Binding CopyCount}</cs:Column>
          <cs:Column Header="Borrower">{Binding Borrower}</cs:Column>
          <cs:Column Header="Reserved By">{Binding ReservedBy}</cs:Column>
          <cs:Column Header="Due">{Binding DueDate}</cs:Column>
          <cs:Column Header="Location">{Binding Location}</cs:Column>
        </cs:Table>
      </div>
    </cs:Region>

    <cs:Region Name="reader-loan-tables" Tag="section" class="split-tables">
      <article class="book-table-card">
        <div class="panel-title"><h2>Readers</h2><span>Active loans, reservations, and fines</span></div>
        <div class="table-scroll">
          <cs:Table Items="{Binding Readers}" Columns="Id:Id,Name:Name,Level:Level,Status:Status,ActiveLoans:Loans,Reservations:Reservations,FineBalance:Fine,Email:Email" />
        </div>
      </article>

      <article class="book-table-card">
        <div class="panel-title"><h2>Loans</h2><span>Borrowing, renewal, return, and overdue state</span></div>
        <div class="table-scroll">
          <cs:Table Items="{Binding Loans}" Columns="Id:Loan,BookTitle:Book,ReaderName:Reader,BorrowedAt:Borrowed,DueAt:Due,ReturnedAt:Returned,Status:Status,RenewCount:Renew,Fine:Fine" />
        </div>
      </article>
    </cs:Region>

    <cs:Region Name="reservation-table" Tag="section" class="book-table-card">
      <div class="panel-title"><h2>Reservations</h2><span>Waiting, completed, and cancelled reservations</span></div>
      <div class="table-scroll">
        <cs:Table Items="{Binding Reservations}" Columns="Id:Id,BookTitle:Book,ReaderName:Reader,CreatedAt:Created,Status:Status" />
      </div>
    </cs:Region>

    <cs:Region Name="inventory-table" Tag="section" class="book-table-card">
      <div class="panel-title"><h2>Inventory Audit</h2><span>Inbound, write-off, and relocation events</span></div>
      <div class="table-scroll">
        <cs:Table Items="{Binding InventoryEvents}" Columns="Id:Id,BookTitle:Book,Type:Type,Quantity:Qty,Reason:Reason,CreatedAt:Created" />
      </div>
    </cs:Region>
  </div>
</cs:Content>
