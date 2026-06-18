<!--
  CodeSpirit Component: DataTable
  Sortable, paginated data table component.
  Usage: <cs:DataTable DataSource="{Binding Items}" Columns="{Binding ColumnDefs}" PageSize="20" />
-->
<div class="cs-component cs-datatable">
  <cs:Conditional Visible="{Binding HasToolbar}">
    <div class="cs-datatable-toolbar">
      <cs:SearchBox Placeholder="Search..." Value="{Binding SearchQuery}" />
      <cs:Button Command="{Binding Export}" Text="Export" />
    </div>
  </cs:Conditional>

  <table>
    <thead>
      <tr>
        <cs:Repeater Items="{Binding Columns}">
          <th cs:sort="{Binding SortField}" class="{Binding SortClass}">
            {Binding Header}
          </th>
        </cs:Repeater>
      </tr>
    </thead>
    <tbody>
      <cs:Repeater Items="{Binding Rows}">
        <tr>
          <cs:Repeater Items="{Binding Cells}">
            <td>{Binding Value}</td>
          </cs:Repeater>
        </tr>
      </cs:Repeater>
    </tbody>
  </table>

  <cs:Pager
    CurrentPage="{Binding CurrentPage}"
    TotalPages="{Binding TotalPages}"
    OnPageChange="{Binding GoToPage}" />
</div>