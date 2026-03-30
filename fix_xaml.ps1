$file = '.\Views\CertificatesListView.xaml'
$lines = Get-Content $file -Encoding UTF8

# Grab up to line 164
$part1 = $lines | Select-Object -First 164

# Missing code to inject
$missing = @"
                                            <Setter Property="Background" Value="#0A000000"/>
                                        </Trigger>
                                        <Trigger Property="IsSelected" Value="True">
                                            <Setter Property="FontWeight" Value="SemiBold"/>
                                        </Trigger>
                                    </Style.Triggers>
                                </Style>
                            </DataGrid.RowStyle>
                            <DataGrid.CellStyle>
                                <Style TargetType="DataGridCell" BasedOn="{StaticResource {x:Type DataGridCell}}">
                                    <Setter Property="BorderThickness" Value="0"/>
                                    <Setter Property="FocusVisualStyle" Value="{x:Null}"/>
                                    <Style.Triggers>
                                        <Trigger Property="IsMouseOver" Value="True">
                                            <Setter Property="Foreground" Value="{DynamicResource PrimaryTextBrush}"/>
                                            <Setter Property="Background" Value="Transparent"/>
                                        </Trigger>
                                        <Trigger Property="IsSelected" Value="True">
                                            <Setter Property="Foreground" Value="White"/>
                                            <Setter Property="Background" Value="{DynamicResource PrimaryBrush}"/>
                                        </Trigger>
                                    </Style.Triggers>
                                </Style>
                            </DataGrid.CellStyle>
                            <DataGrid.Columns>
                                <DataGridTextColumn Header="ت" Binding="{Binding Sequence}" Width="45" ElementStyle="{DynamicResource CenterAlignmentStyle}"/>
                                <DataGridTextColumn Header="رقم الشهادة" Binding="{Binding CertificateNumber}" Width="135" FontWeight="SemiBold" ElementStyle="{DynamicResource CenterAlignmentStyle}"/>
                                <DataGridTextColumn Header="عدد العينات" Binding="{Binding SampleCount}" Width="85" ElementStyle="{DynamicResource CenterAlignmentStyle}"><DataGridTextColumn.HeaderStyle><Style TargetType="DataGridColumnHeader" BasedOn="{StaticResource {x:Type DataGridColumnHeader}}"><Setter Property="HorizontalContentAlignment" Value="Center"/></Style></DataGridTextColumn.HeaderStyle></DataGridTextColumn>
                                <DataGridTemplateColumn Header="الجهة المرسلة" Width="3*" MinWidth="180">
                                    <DataGridTemplateColumn.CellTemplate>
                                        <DataTemplate>
                                            <TextBlock Text="{Binding Sender}" TextWrapping="Wrap" TextAlignment="Right" VerticalAlignment="Center" Padding="5,2"
                                                       Foreground="{Binding Foreground, RelativeSource={RelativeSource AncestorType=DataGridCell}}"/>
                                        </DataTemplate>
                                    </DataGridTemplateColumn.CellTemplate>
                                </DataGridTemplateColumn>
                                <DataGridTextColumn Header="المورد" Binding="{Binding Supplier}" Width="2*" MinWidth="120" ElementStyle="{DynamicResource RightAlignmentStyle}"/>

                                <DataGridTextColumn Header="رقم الاخطار" Binding="{Binding NotificationNumber}" Width="100" ElementStyle="{DynamicResource CenterAlignmentStyle}"/>
                                <DataGridTextColumn Header="رقم الإيصال" Binding="{Binding FinancialReceiptNumber}" Width="100" ElementStyle="{DynamicResource CenterAlignmentStyle}"/>
                                <DataGridTextColumn Header="تاريخ الإصدار" Binding="{Binding IssueDate, StringFormat=yyyy/MM/dd HH:mm}" Width="120" ElementStyle="{DynamicResource CenterAlignmentStyle}"/>
                            </DataGrid.Columns>
                        </DataGrid>
                    </Border>
                    
                    <!-- Unified Totals & Pagination Footer Bar (Matches Sample Receptions) -->
                    <Border Grid.Row="3" Background="{DynamicResource CardBackgroundBrush}" CornerRadius="12" Padding="20" Margin="0,20,0,0" BorderBrush="{DynamicResource BorderBrush}" BorderThickness="1">
                        <Grid>
                            <Grid.ColumnDefinitions>
                                <ColumnDefinition Width="Auto"/> <!-- Pagination -->
                                <ColumnDefinition Width="Auto"/> <!-- Actions -->
                                <ColumnDefinition Width="*"/>    <!-- Spacer -->
                                <ColumnDefinition Width="Auto"/> <!-- Totals -->
                            </Grid.ColumnDefinitions>
                            
                            <!-- Pagination (Left) -->
                            <StackPanel Grid.Column="0" Orientation="Horizontal" VerticalAlignment="Center" Margin="0,0,20,0">
"@

# Grab the rest from line 165
$part2 = $lines | Select-Object -Skip 164

# Write back
$part1 + $missing.Split("`n").Replace("`r","") + $part2 | Set-Content $file -Encoding UTF8
