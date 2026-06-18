<!--
  CodeSpirit Component: DataCard
  A reusable card component for displaying key metrics.
  Usage: <cs:DataCard Title="Users" Value="1,234" Icon="users" Trend="+12%" />
-->
<div class="cs-component cs-datacard">
  <div class="cs-datacard-icon">
    <cs:Icon Name="{Binding Icon}" />
  </div>
  <div class="cs-datacard-body">
    <span class="cs-datacard-title">{Binding Title}</span>
    <span class="cs-datacard-value">{Binding Value}</span>
    <cs:Conditional Visible="{Binding HasTrend}">
      <span class="cs-datacard-trend {Binding TrendClass}">{Binding Trend}</span>
    </cs:Conditional>
  </div>
</div>