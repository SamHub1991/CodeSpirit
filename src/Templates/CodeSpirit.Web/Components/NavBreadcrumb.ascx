<!--
  CodeSpirit Component: NavBreadcrumb
  Breadcrumb navigation component.
  Usage: <cs:NavBreadcrumb Items="{Binding Breadcrumbs}" />
-->
<nav aria-label="Breadcrumb" class="cs-component cs-breadcrumb">
  <ol>
    <cs:Repeater Items="{Binding Items}">
      <li>
        <cs:Conditional Visible="{Binding IsLast}">
          <span aria-current="page">{Binding Label}</span>
        </cs:Conditional>
        <cs:Conditional Visible="{Binding IsNotLast}">
          <a href="{Binding Url}">{Binding Label}</a>
          <span class="cs-breadcrumb-sep">/</span>
        </cs:Conditional>
      </li>
    </cs:Repeater>
  </ol>
</nav>