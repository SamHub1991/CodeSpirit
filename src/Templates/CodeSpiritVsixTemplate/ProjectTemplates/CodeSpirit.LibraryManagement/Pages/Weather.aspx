<%@ Page %>

<cs:Content PlaceHolder="Body">
  <div class="weather-page">
    <h1>Weather Forecast</h1>

    <cs:Form>
      <div class="form-group">
        <cs:Field Name="City" Label="City" />
        <cs:Button Command="Refresh">Search</cs:Button>
      </div>
    </cs:Form>

    <cs:Conditional Visible="{Binding HasForecast}">
      <cs:Table class="weather-table" Items="{Binding Forecast}" Columns="Date:Date:yyyy-MM-dd,TemperatureC:Temp (C),TemperatureF:Temp (F),Summary:Summary" />
    </cs:Conditional>
  </div>
</cs:Content>
