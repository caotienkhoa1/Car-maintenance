using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using BE.DTOs.MaintenanceTicket;
using BE.DTOs.ServiceTask;
using BE.DTOs.TicketComponent;
using BE.DTOs.TotalReceipt;
using BE.interfaces;
using BE.repository.IRepository;

namespace BE.services
{
    public class ReportService : IReportService
    {
        private readonly IMaintenanceTicketService _maintenanceTicketService;
        private readonly IServiceTaskService _serviceTaskService;
        private readonly ITicketComponentService _ticketComponentService;
        private readonly IHistoryLogRepository _historyLogRepository;
        private readonly ITotalReceiptService _totalReceiptService;

        public ReportService(
            IMaintenanceTicketService maintenanceTicketService,
            IServiceTaskService serviceTaskService,
            ITicketComponentService ticketComponentService,
            IHistoryLogRepository historyLogRepository,
            ITotalReceiptService totalReceiptService)
        {
            _maintenanceTicketService = maintenanceTicketService;
            _serviceTaskService = serviceTaskService;
            _ticketComponentService = ticketComponentService;
            _historyLogRepository = historyLogRepository;
            _totalReceiptService = totalReceiptService;
        }

        public async Task<byte[]> GenerateQuotationPdfAsync(long maintenanceTicketId)
        {
            return await GenerateMaintenanceTicketPdfWithTypeAsync(maintenanceTicketId, "QUOTATION");
        }

        public async Task<byte[]> GenerateProvisionalInvoicePdfAsync(long maintenanceTicketId)
        {
            return await GenerateMaintenanceTicketPdfWithTypeAsync(maintenanceTicketId, "PROVISIONAL");
        }

        private async Task<byte[]> GenerateMaintenanceTicketPdfWithTypeAsync(long maintenanceTicketId, string type)
        {
            QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;

            var ticket = await _maintenanceTicketService.GetMaintenanceTicketByIdAsync(maintenanceTicketId);
            if (ticket == null) throw new ArgumentException("Maintenance ticket not found");

            var serviceTasks = await _serviceTaskService.GetServiceTasksByMaintenanceTicketIdAsync(maintenanceTicketId);
            var components = await _ticketComponentService.GetByMaintenanceTicketIdAsync(maintenanceTicketId);

            string title = type == "QUOTATION" ? "PHIẾU BÁO GIÁ DỊCH VỤ" : "PHIẾU TẠM TÍNH CHI PHÍ";
            string codePrefix = type == "QUOTATION" ? "BG" : "TT";
            string note = type == "QUOTATION" 
                ? "Giá trên là báo giá dự kiến dựa trên tình trạng kiểm tra ban đầu (Giá chưa bao gồm thuế VAT). Nếu có phát sinh thêm trong quá trình thực hiện, chúng tôi sẽ thông báo trước cho quý khách."
                : "Quý khách vui lòng kiểm tra kỹ danh sách trên trước khi thanh toán chính thức tại quầy thu ngân (Giá chưa bao gồm thuế VAT).";

            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(1.5f, Unit.Centimetre);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(10));

                    // Header: Logo/Info (left) + Title (right) - Giống Receipt
                    page.Header().PaddingBottom(10).Row(row =>
                    {
                        row.RelativeItem(3).Column(col =>
                        {
                            col.Item().Text(ticket.BranchName ?? "APMMS GARAGE").FontSize(11).Bold();
                            col.Item().Text(ticket.BranchAddress ?? "Địa chỉ chi nhánh").FontSize(9);
                        });
                        row.RelativeItem(2).AlignRight().Column(col =>
                        {
                            col.Item().Text(title).FontSize(14).Bold();
                            col.Item().Text($"Ngày: {DateTime.Now:dd/MM/yyyy HH:mm}").FontSize(9);
                            col.Item().Text($"Số: {codePrefix}-{ticket.Code ?? ticket.Id.ToString()}").FontSize(9).Bold();
                        });
                    });

                    page.Content().Column(column =>
                    {
                        // Advisor info line
                        column.Item()
                            .PaddingTop(5)
                            .PaddingBottom(10)
                            .BorderBottom(0.5f)
                            .BorderColor(Colors.Grey.Lighten2)
                            .Row(row =>
                            {
                                row.RelativeItem().Text($"Cố vấn dịch vụ: {ticket.ConsulterName ?? "-"}").FontSize(9);
                                row.RelativeItem().AlignRight().Text($"Biển số xe: {ticket.LicensePlate ?? "-"}").FontSize(9);
                            });

                        column.Item().PaddingTop(10);

                        // Customer and Vehicle Info - 2 columns layout giống Receipt
                        column.Item().Row(row =>
                        {
                            // Customer Info
                            row.RelativeItem().Column(customerCol =>
                            {
                                customerCol.Item().PaddingBottom(5).Text("Thông tin khách hàng").FontSize(11).Bold();
                                customerCol.Item().Table(table =>
                                {
                                    table.ColumnsDefinition(columns => { columns.RelativeColumn(2); columns.RelativeColumn(3); });
                                    table.Cell().Element(CellStyle).Text("Tên:").FontSize(9);
                                    table.Cell().Element(CellStyle).Text(ticket.CustomerName ?? "-").FontSize(9);
                                    table.Cell().Element(CellStyle).Text("SDT:").FontSize(9);
                                    table.Cell().Element(CellStyle).Text(ticket.CustomerPhone ?? "-").FontSize(9);
                                    table.Cell().Element(CellStyle).Text("Địa chỉ:").FontSize(9);
                                    table.Cell().Element(CellStyle).Text(ticket.CustomerAddress ?? "-").FontSize(9);
                                });
                            });

                            row.ConstantItem(20);

                            // Vehicle Info
                            row.RelativeItem().Column(vehicleCol =>
                            {
                                vehicleCol.Item().PaddingBottom(5).Text("Thông tin xe").FontSize(11).Bold();
                                vehicleCol.Item().Table(table =>
                                {
                                    table.ColumnsDefinition(columns => { columns.RelativeColumn(2); columns.RelativeColumn(3); });
                                    table.Cell().Element(CellStyle).Text("Tên xe:").FontSize(9);
                                    table.Cell().Element(CellStyle).Text($"{ticket.CarModel ?? "-"}").FontSize(9);
                                    table.Cell().Element(CellStyle).Text("Loại xe:").FontSize(9);
                                    table.Cell().Element(CellStyle).Text(ticket.VehicleType ?? "-").FontSize(9);
                                    table.Cell().Element(CellStyle).Text("Số km:").FontSize(9);
                                    table.Cell().Element(CellStyle).Text(ticket.Mileage?.ToString("N0") ?? "-").FontSize(9);
                                });
                            });
                        });

                        column.Item().PaddingTop(15);

                        // Tasks table - Sử dụng HeaderCellStyle giống Receipt
                        if (serviceTasks != null && serviceTasks.Any())
                        {
                            column.Item().PaddingBottom(5).Text("Danh sách công việc").FontSize(10).Bold();
                            column.Item().Table(table =>
                            {
                                table.ColumnsDefinition(cols =>
                                {
                                    cols.ConstantColumn(30);
                                    cols.RelativeColumn();
                                    cols.ConstantColumn(120);
                                });
                                table.Header(h =>
                                {
                                    h.Cell().Element(HeaderCellStyle).Text("STT").FontSize(9).Bold();
                                    h.Cell().Element(HeaderCellStyle).Text("Nội dung công việc").FontSize(9).Bold();
                                    h.Cell().Element(HeaderCellStyle).AlignRight().Text("Phí nhân công").FontSize(9).Bold();
                                });
                                int stt = 1;
                                foreach (var task in serviceTasks)
                                {
                                    table.Cell().Element(CellStyle).AlignCenter().Text(stt++.ToString()).FontSize(9);
                                    table.Cell().Element(CellStyle).Text(task.TaskName).FontSize(9);
                                    table.Cell().Element(CellStyle).AlignRight().Text(FormatCurrency(task.LaborCost ?? 0)).FontSize(9);
                                }
                            });
                            column.Item().PaddingTop(10);
                        }

                        // Components table - Sử dụng HeaderCellStyle giống Receipt
                        if (components != null && components.Any())
                        {
                            column.Item().PaddingBottom(5).Text("Phụ tùng dự kiến").FontSize(10).Bold();
                            column.Item().Table(table =>
                            {
                                table.ColumnsDefinition(cols =>
                                {
                                    cols.ConstantColumn(30);
                                    cols.RelativeColumn();
                                    cols.ConstantColumn(60);
                                    cols.ConstantColumn(100);
                                    cols.ConstantColumn(120);
                                });
                                table.Header(h =>
                                {
                                    h.Cell().Element(HeaderCellStyle).Text("STT").FontSize(9).Bold();
                                    h.Cell().Element(HeaderCellStyle).Text("Tên phụ tùng").FontSize(9).Bold();
                                    h.Cell().Element(HeaderCellStyle).AlignCenter().Text("SL").FontSize(9).Bold();
                                    h.Cell().Element(HeaderCellStyle).AlignRight().Text("Đơn giá").FontSize(9).Bold();
                                    h.Cell().Element(HeaderCellStyle).AlignRight().Text("Thành tiền").FontSize(9).Bold();
                                });
                                int stt = 1;
                                foreach (var comp in components)
                                {
                                    decimal qty = comp.ActualQuantity ?? (decimal)comp.Quantity;
                                    decimal price = comp.UnitPrice ?? 0;
                                    decimal total = qty * price;
                                    table.Cell().Element(CellStyle).AlignCenter().Text(stt++.ToString()).FontSize(9);
                                    table.Cell().Element(CellStyle).Text(comp.ComponentName).FontSize(9);
                                    table.Cell().Element(CellStyle).AlignCenter().Text(qty.ToString("G29")).FontSize(9);
                                    table.Cell().Element(CellStyle).AlignRight().Text(FormatCurrency(price)).FontSize(9);
                                    table.Cell().Element(CellStyle).AlignRight().Text(FormatCurrency(total)).FontSize(9);
                                }
                            });
                            column.Item().PaddingTop(10);
                        }

                        // Summary section - Giống layout Receipt
                        var totalLabor = serviceTasks?.Sum(t => t.LaborCost ?? 0) ?? 0;
                        var totalComp = components?.Sum(c => (c.ActualQuantity ?? (decimal)c.Quantity) * (c.UnitPrice ?? 0)) ?? 0;
                        var grandTotal = totalLabor + totalComp;

                        column.Item().PaddingTop(5).Table(summaryTable =>
                        {
                            summaryTable.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn(2);
                                columns.ConstantColumn(150);
                            });

                            summaryTable.Cell().Element(CellStyle).Text("Tổng phí nhân công").FontSize(9);
                            summaryTable.Cell().Element(CellStyle).AlignRight().Text(FormatCurrency(totalLabor)).FontSize(9);

                            summaryTable.Cell().Element(CellStyle).Text("Tổng chi phí phụ tùng").FontSize(9);
                            summaryTable.Cell().Element(CellStyle).AlignRight().Text(FormatCurrency(totalComp)).FontSize(9);

                            summaryTable.Cell().Element(HeaderCellStyle).Text("TỔNG CỘNG DỰ KIẾN").FontSize(9).Bold();
                            summaryTable.Cell().Element(HeaderCellStyle).AlignRight().Text(FormatCurrency(grandTotal)).FontSize(9).Bold();
                        });

                        // Note section
                        column.Item().PaddingTop(15).Text(x =>
                        {
                            x.Span("Ghi chú: ").Bold().FontSize(9);
                            x.Span(note).Italic().FontSize(9);
                        });

                        // Signatures section - Giống Receipt
                        column.Item().PaddingTop(30).Row(row =>
                        {
                            row.RelativeItem().Column(c =>
                            {
                                c.Item().AlignCenter().Text(type == "QUOTATION" ? "KHÁCH HÀNG XÁC NHẬN" : "KHÁCH HÀNG").FontSize(9).Bold();
                                c.Item().AlignCenter().Text("(Ký và ghi rõ họ tên)").FontSize(8).Italic();
                            });
                            row.RelativeItem().Column(c =>
                            {
                                c.Item().AlignCenter().Text("NHÂN VIÊN TƯ VẤN").FontSize(9).Bold();
                                c.Item().AlignCenter().Text("(Ký và ghi rõ họ tên)").FontSize(8).Italic();
                            });
                        });
                    });

                    page.Footer().AlignCenter().Text(x =>
                    {
                        x.Span("Trang ").FontSize(8).FontColor(Colors.Grey.Medium);
                        x.CurrentPageNumber().FontSize(8).FontColor(Colors.Grey.Medium);
                    });
                });
            });

            return document.GeneratePdf();
        }

        public async Task<byte[]> GenerateMaintenanceTicketPdfAsync(long maintenanceTicketId)
        {
            QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;

            // Lấy thông tin phiếu bảo dưỡng
            var ticket = await _maintenanceTicketService.GetMaintenanceTicketByIdAsync(maintenanceTicketId);
            if (ticket == null)
                throw new ArgumentException("Maintenance ticket not found");

            // Lấy danh sách công việc
            var serviceTasks = await _serviceTaskService.GetServiceTasksByMaintenanceTicketIdAsync(maintenanceTicketId);
            
            // Lấy danh sách phụ tùng
            var components = await _ticketComponentService.GetByMaintenanceTicketIdAsync(maintenanceTicketId);
            
            // Lấy lịch sử thay đổi
            var historyLogs = await _historyLogRepository.GetByMaintenanceTicketIdAsync(maintenanceTicketId);

            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(2, Unit.Centimetre);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(10));

                    page.Header()
                        .Column(column =>
                        {
                            column.Item().Row(row =>
                            {
                                row.RelativeItem().Column(col =>
                                {
                                    col.Item().Text("PHIẾU BẢO DƯỠNG XE").FontSize(20).Bold();
                                    col.Item().Text($"Mã phiếu: {ticket.Code ?? "N/A"}").FontSize(12);
                                });
                                row.ConstantItem(80).AlignRight().Column(col =>
                                {
                                    col.Item().Text($"Ngày tạo: {ticket.CreatedDate:dd/MM/yyyy}").FontSize(10);
                                    if (ticket.StartTime.HasValue)
                                        col.Item().Text($"Bắt đầu: {ticket.StartTime:dd/MM/yyyy HH:mm}").FontSize(10);
                                    if (ticket.EndTime.HasValue)
                                        col.Item().Text($"Kết thúc: {ticket.EndTime:dd/MM/yyyy HH:mm}").FontSize(10);
                                });
                            });
                        });

                    page.Content()
                        .PaddingVertical(1, Unit.Centimetre)
                        .Column(column =>
                        {
                            // Thông tin khách hàng và xe
                            column.Item().PaddingBottom(10).Column(infoCol =>
                            {
                                infoCol.Item().Text("THÔNG TIN KHÁCH HÀNG VÀ XE").FontSize(14).Bold();
                                infoCol.Item().PaddingTop(5).Table(table =>
                                {
                                    table.ColumnsDefinition(columns =>
                                    {
                                        columns.RelativeColumn();
                                        columns.RelativeColumn();
                                    });

                                    table.Cell().Element(CellStyle).Text("Khách hàng:").FontSize(10);
                                    table.Cell().Element(CellStyle).Text(ticket.CustomerName ?? "N/A").FontSize(10);

                                    table.Cell().Element(CellStyle).Text("Số điện thoại:").FontSize(10);
                                    table.Cell().Element(CellStyle).Text(ticket.CustomerPhone ?? "N/A").FontSize(10);

                                    table.Cell().Element(CellStyle).Text("Email:").FontSize(10);
                                    table.Cell().Element(CellStyle).Text(ticket.CustomerEmail ?? "N/A").FontSize(10);

                                    table.Cell().Element(CellStyle).Text("Địa chỉ:").FontSize(10);
                                    table.Cell().Element(CellStyle).Text(ticket.CustomerAddress ?? "N/A").FontSize(10);

                                    table.Cell().Element(CellStyle).Text("Biển số xe:").FontSize(10);
                                    table.Cell().Element(CellStyle).Text(ticket.LicensePlate ?? "N/A").FontSize(10);

                                    table.Cell().Element(CellStyle).Text("Loại xe:").FontSize(10);
                                    table.Cell().Element(CellStyle).Text(ticket.VehicleType ?? "N/A").FontSize(10);

                                    table.Cell().Element(CellStyle).Text("Mẫu xe:").FontSize(10);
                                    table.Cell().Element(CellStyle).Text(ticket.CarModel ?? "N/A").FontSize(10);

                                    table.Cell().Element(CellStyle).Text("Số khung:").FontSize(10);
                                    table.Cell().Element(CellStyle).Text(ticket.VinNumber ?? "N/A").FontSize(10);

                                    table.Cell().Element(CellStyle).Text("Số km:").FontSize(10);
                                    table.Cell().Element(CellStyle).Text(ticket.Mileage.HasValue ? ticket.Mileage.Value.ToString() : "N/A").FontSize(10);
                                });
                            });

                            // Thông tin phiếu bảo dưỡng
                            column.Item().PaddingBottom(10).Column(infoCol =>
                            {
                                infoCol.Item().Text("THÔNG TIN PHIẾU BẢO DƯỠNG").FontSize(14).Bold();
                                infoCol.Item().PaddingTop(5).Table(table =>
                                {
                                    table.ColumnsDefinition(columns =>
                                    {
                                        columns.RelativeColumn();
                                        columns.RelativeColumn();
                                    });

                                    table.Cell().Element(CellStyle).Text("Trạng thái:").FontSize(10);
                                    table.Cell().Element(CellStyle).Text(GetStatusName(ticket.StatusCode)).FontSize(10);

                                    table.Cell().Element(CellStyle).Text("Chi nhánh:").FontSize(10);
                                    table.Cell().Element(CellStyle).Text(ticket.BranchName ?? "N/A").FontSize(10);

                                    table.Cell().Element(CellStyle).Text("Tư vấn viên:").FontSize(10);
                                    table.Cell().Element(CellStyle).Text(ticket.ConsulterName ?? "N/A").FontSize(10);

                                    if (ticket.Technicians != null && ticket.Technicians.Any())
                                    {
                                        table.Cell().Element(CellStyle).Text("Kỹ thuật viên:").FontSize(10);
                                        table.Cell().Element(CellStyle).Text(string.Join(", ", ticket.Technicians.Select(t => t.TechnicianName))).FontSize(10);
                                    }

                                    table.Cell().Element(CellStyle).Text("Mô tả:").FontSize(10);
                                    table.Cell().Element(CellStyle).Text(ticket.Description ?? "N/A").FontSize(10);
                                });
                            });

                            // Danh sách công việc
                            if (serviceTasks != null && serviceTasks.Any())
                            {
                                column.Item().PaddingBottom(10).Column(taskCol =>
                                {
                                    taskCol.Item().Text("DANH SÁCH CÔNG VIỆC").FontSize(14).Bold();
                                    taskCol.Item().PaddingTop(5).Table(table =>
                                    {
                                        table.ColumnsDefinition(columns =>
                                        {
                                            columns.RelativeColumn(2);
                                            columns.RelativeColumn();
                                            columns.RelativeColumn();
                                            columns.RelativeColumn();
                                            columns.RelativeColumn();
                                        });

                                        // Header
                                        table.Cell().Element(CellStyle).Text("Tên công việc").FontSize(10).Bold();
                                        table.Cell().Element(CellStyle).Text("Thời gian chuẩn").FontSize(10).Bold();
                                        table.Cell().Element(CellStyle).Text("Thời gian thực tế").FontSize(10).Bold();
                                        table.Cell().Element(CellStyle).Text("Trạng thái").FontSize(10).Bold();
                                        table.Cell().Element(CellStyle).Text("Phí nhân công").FontSize(10).Bold();

                                        // Rows
                                        foreach (var task in serviceTasks)
                                        {
                                            table.Cell().Element(CellStyle).Text(task.TaskName ?? "N/A").FontSize(9);
                                            table.Cell().Element(CellStyle).Text(task.StandardLaborTime.HasValue ? task.StandardLaborTime.Value.ToString("F2") + "h" : "N/A").FontSize(9);
                                            table.Cell().Element(CellStyle).Text(task.ActualLaborTime.HasValue ? task.ActualLaborTime.Value.ToString("F2") + "h" : "N/A").FontSize(9);
                                            table.Cell().Element(CellStyle).Text(GetTaskStatusName(task.StatusCode)).FontSize(9);
                                            table.Cell().Element(CellStyle).Text(task.LaborCost.HasValue ? task.LaborCost.Value.ToString("N0") + " ₫" : "N/A").FontSize(9);
                                        }
                                    });
                                });
                            }

                            // Danh sách phụ tùng
                            if (components != null && components.Any())
                            {
                                column.Item().PaddingBottom(10).Column(compCol =>
                                {
                                    compCol.Item().Text("DANH SÁCH PHỤ TÙNG").FontSize(14).Bold();
                                    compCol.Item().PaddingTop(5).Table(table =>
                                    {
                                        table.ColumnsDefinition(columns =>
                                        {
                                            columns.RelativeColumn(2);
                                            columns.RelativeColumn();
                                            columns.RelativeColumn();
                                            columns.RelativeColumn();
                                            columns.RelativeColumn();
                                        });

                                        // Header
                                        table.Cell().Element(CellStyle).Text("Tên phụ tùng").FontSize(10).Bold();
                                        table.Cell().Element(CellStyle).Text("Số lượng").FontSize(10).Bold();
                                        table.Cell().Element(CellStyle).Text("Số lượng thực tế").FontSize(10).Bold();
                                        table.Cell().Element(CellStyle).Text("Đơn giá").FontSize(10).Bold();
                                        table.Cell().Element(CellStyle).Text("Thành tiền").FontSize(10).Bold();

                                        // Rows
                                        decimal totalComponentCost = 0;
                                        foreach (var comp in components)
                                        {
                                            var quantity = comp.ActualQuantity.HasValue ? comp.ActualQuantity.Value : comp.Quantity;
                                            var unitPrice = comp.UnitPrice.HasValue ? comp.UnitPrice.Value : 0;
                                            var total = quantity * unitPrice;
                                            totalComponentCost += total;

                                            table.Cell().Element(CellStyle).Text(comp.ComponentName ?? "N/A").FontSize(9);
                                            table.Cell().Element(CellStyle).Text(comp.Quantity.ToString()).FontSize(9);
                                            table.Cell().Element(CellStyle).Text(comp.ActualQuantity.HasValue ? comp.ActualQuantity.Value.ToString() : comp.Quantity.ToString()).FontSize(9);
                                            table.Cell().Element(CellStyle).Text(unitPrice.ToString("N0") + " ₫").FontSize(9);
                                            table.Cell().Element(CellStyle).Text(total.ToString("N0") + " ₫").FontSize(9);
                                        }

                                        // Total row
                                        table.Cell().ColumnSpan(4).Element(CellStyle).Text("Tổng tiền phụ tùng:").FontSize(10).Bold().AlignRight();
                                        table.Cell().Element(CellStyle).Text(totalComponentCost.ToString("N0") + " ₫").FontSize(10).Bold();
                                    });
                                });
                            }

                            // Tổng kết chi phí
                            column.Item().PaddingBottom(10).Column(summaryCol =>
                            {
                                summaryCol.Item().Text("TỔNG KẾT CHI PHÍ").FontSize(14).Bold();
                                summaryCol.Item().PaddingTop(5).Table(table =>
                                {
                                    table.ColumnsDefinition(columns =>
                                    {
                                        columns.RelativeColumn();
                                        columns.RelativeColumn();
                                    });

                                    var totalLaborCost = serviceTasks?.Sum(t => t.LaborCost.HasValue ? t.LaborCost.Value : 0) ?? 0;
                                    var totalComponentCost = components?.Sum(c => {
                                        var qty = c.ActualQuantity.HasValue ? c.ActualQuantity.Value : c.Quantity;
                                        var price = c.UnitPrice.HasValue ? c.UnitPrice.Value : 0;
                                        return qty * price;
                                    }) ?? 0;
                                    var totalCost = totalLaborCost + totalComponentCost;

                                    table.Cell().Element(CellStyle).Text("Tổng phí nhân công:").FontSize(10);
                                    table.Cell().Element(CellStyle).Text(totalLaborCost.ToString("N0") + " ₫").FontSize(10);

                                    table.Cell().Element(CellStyle).Text("Tổng tiền phụ tùng:").FontSize(10);
                                    table.Cell().Element(CellStyle).Text(totalComponentCost.ToString("N0") + " ₫").FontSize(10);

                                    table.Cell().Element(CellStyle).Text("TỔNG CỘNG:").FontSize(12).Bold();
                                    table.Cell().Element(CellStyle).Text(totalCost.ToString("N0") + " ₫").FontSize(12).Bold();
                                });
                            });

                            // Lịch sử thay đổi
                            if (historyLogs != null && historyLogs.Any())
                            {
                                column.Item().PaddingBottom(10).Column(historyCol =>
                                {
                                    historyCol.Item().Text("LỊCH SỬ THAY ĐỔI").FontSize(14).Bold();
                                    historyCol.Item().PaddingTop(5).Column(logCol =>
                                    {
                                        var sortedLogs = historyLogs.OrderByDescending(l => l.CreatedAt ?? DateTime.MinValue).ToList();
                                        foreach (var log in sortedLogs)
                                        {
                                            logCol.Item().PaddingBottom(5).Column(item =>
                                            {
                                                item.Item().Row(row =>
                                                {
                                                    row.RelativeItem().Text($"{log.CreatedAt:dd/MM/yyyy HH:mm} - {GetActionName(log.Action)}").FontSize(9).Bold();
                                                });
                                                if (!string.IsNullOrEmpty(log.NewData))
                                                {
                                                    item.Item().PaddingLeft(10).Text(log.NewData).FontSize(8);
                                                }
                                                if (!string.IsNullOrEmpty(log.OldData))
                                                {
                                                    item.Item().PaddingLeft(10).Text($"Trước: {log.OldData}").FontSize(8).FontColor(Colors.Grey.Medium);
                                                }
                                            });
                                        }
                                    });
                                });
                            }
                        });

                    page.Footer()
                        .AlignCenter()
                        .Text(x =>
                        {
                            x.Span("Trang ").FontSize(9).FontColor(Colors.Grey.Medium);
                            x.CurrentPageNumber().FontSize(9).FontColor(Colors.Grey.Medium);
                            x.Span(" / ").FontSize(9).FontColor(Colors.Grey.Medium);
                            x.TotalPages().FontSize(9).FontColor(Colors.Grey.Medium);
                        });
                });
            });

            return document.GeneratePdf();
        }

        private static IContainer CellStyle(IContainer container)
        {
            return container
                .BorderBottom(0.5f)
                .BorderColor(Colors.Grey.Lighten2)
                .Padding(5)
                .Background(Colors.White);
        }

        private static string GetStatusName(string? statusCode)
        {
            return statusCode switch
            {
                "PENDING" => "Chờ xử lý",
                "ASSIGNED" => "Đã gán",
                "IN_PROGRESS" => "Đang thực hiện",
                "COMPLETED" => "Hoàn thành",
                "CANCELLED" => "Đã hủy",
                _ => statusCode ?? "N/A"
            };
        }

        private static string GetTaskStatusName(string? statusCode)
        {
            return statusCode switch
            {
                "PENDING" => "Chờ",
                "IN_PROGRESS" => "Đang làm",
                "DONE" => "Hoàn thành",
                "COMPLETED" => "Hoàn thành",
                "CANCELLED" => "Đã hủy",
                _ => statusCode ?? "N/A"
            };
        }

        private static string GetActionName(string? action)
        {
            return action switch
            {
                "CREATE_MAINTENANCE_TICKET" => "Tạo phiếu bảo dưỡng",
                "UPDATE_STATUS" => "Cập nhật trạng thái",
                "ASSIGN_TECHNICIAN" => "Gán kỹ thuật viên",
                "ADD_SERVICE_TASK" => "Thêm công việc",
                "UPDATE_SERVICE_TASK" => "Cập nhật công việc",
                "DELETE_SERVICE_TASK" => "Xóa công việc",
                "ADD_COMPONENT" => "Thêm phụ tùng",
                "UPDATE_COMPONENT" => "Cập nhật phụ tùng",
                "DELETE_COMPONENT" => "Xóa phụ tùng",
                "UPDATE_COMPONENT_QUANTITY" => "Cập nhật số lượng phụ tùng",
                "ASSIGN_SERVICE_TASK_TECHNICIANS" => "Gán kỹ thuật viên cho công việc",
                "WARNING_LABOR_TIME_EXCEEDED" => "Cảnh báo thời gian",
                _ => action ?? "Thay đổi"
            };
        }

        public async Task<byte[]> GenerateTotalReceiptPdfAsync(long totalReceiptId)
        {
            try
            {
                QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;

                // Lấy thông tin hóa đơn
                var receipt = await _totalReceiptService.GetByIdAsync(totalReceiptId);
                if (receipt == null)
                    throw new ArgumentException("Total receipt not found");

                // Lấy danh sách công việc và phụ tùng từ maintenance ticket
                var tasks = new List<ServiceTaskListResponseDto>();
                var components = new List<BE.DTOs.TicketComponent.ResponseDto>();
                
                if (receipt.MaintenanceTicketId.HasValue)
                {
                    try
                    {
                        tasks = (await _serviceTaskService.GetServiceTasksByMaintenanceTicketIdAsync(receipt.MaintenanceTicketId.Value)).ToList();
                    }
                    catch (Exception)
                    {
                        tasks = new List<ServiceTaskListResponseDto>();
                    }
                    
                    try
                    {
                        components = (await _ticketComponentService.GetByMaintenanceTicketIdAsync(receipt.MaintenanceTicketId.Value)).ToList();
                    }
                    catch (Exception)
                    {
                        components = new List<BE.DTOs.TicketComponent.ResponseDto>();
                    }
                }

            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(1.5f, Unit.Centimetre);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(10));

                    // Header: Company info (left) + Invoice title (right)
                    page.Header()
                        .PaddingBottom(10)
                        .Row(row =>
                        {
                            row.RelativeItem(3).Column(col =>
                            {
                                col.Item().Text(receipt.BranchName ?? "CÔNG TY BẢO DƯỠNG XE").FontSize(11).Bold();
                                col.Item().Text(receipt.BranchAddress ?? "Địa chỉ chi nhánh").FontSize(9);
                            });
                            row.RelativeItem(2).AlignRight().Column(col =>
                            {
                                col.Item().Text("HÓA ĐƠN DỊCH VỤ").FontSize(14).Bold();
                                col.Item().Text($"Ngày {(receipt.CreatedAt?.ToString("dd/MM/yyyy") ?? DateTime.Now.ToString("dd/MM/yyyy"))}").FontSize(9);
                                col.Item().Text($"Số: {receipt.Code ?? "N/A"}").FontSize(9).Bold();
                            });
                        });

                    // Content - chỉ gọi một lần
                    page.Content()
                        .Column(column =>
                        {
                            // Service advisor and dates
                            column.Item()
                                .PaddingTop(5)
                                .PaddingBottom(10)
                                .BorderBottom(0.5f)
                                .BorderColor(Colors.Grey.Lighten2)
                                .Row(row =>
                                {
                                    row.RelativeItem().Column(col =>
                                    {
                                        col.Item().Text($"Cố vấn dịch vụ: {receipt.AccountantName ?? "-"}").FontSize(9);
                                        col.Item().Text($"Điện thoại: {receipt.BranchPhone ?? "-"}").FontSize(9);
                                    });
                                    row.RelativeItem().AlignRight().Column(col =>
                                    {
                                        col.Item().Text($"Ngày sửa: {(receipt.MaintenanceStartTime?.ToString("dd/MM/yyyy") ?? "-")}").FontSize(9);
                                        col.Item().Text($"Ngày hoàn thành: {(receipt.MaintenanceEndTime?.ToString("dd/MM/yyyy") ?? "-")}").FontSize(9);
                                    });
                                });

                            column.Item().PaddingTop(10);

                            // Customer and Vehicle Info
                            column.Item().Row(row =>
                            {
                                // Customer Info
                                row.RelativeItem().Column(customerCol =>
                                {
                                    customerCol.Item().PaddingBottom(5).Text("Thông tin khách hàng").FontSize(11).Bold();
                                    customerCol.Item().Table(table =>
                                    {
                                        table.ColumnsDefinition(columns =>
                                        {
                                            columns.RelativeColumn(2);
                                            columns.RelativeColumn(3);
                                        });
                                        
                                        // Chuẩn hóa giới tính giống như trên web
                                        string customerGender = "-";
                                        if (!string.IsNullOrEmpty(receipt.CustomerGender))
                                        {
                                            if (receipt.CustomerGender == "Male" || receipt.CustomerGender == "Nam")
                                                customerGender = "Nam";
                                            else if (receipt.CustomerGender == "Female" || receipt.CustomerGender == "Nữ")
                                                customerGender = "Nữ";
                                            else
                                                customerGender = receipt.CustomerGender;
                                        }
                                        
                                        table.Cell().Element(CellStyle).Text("Tên:").FontSize(9);
                                        table.Cell().Element(CellStyle).Text(receipt.CustomerName ?? "-").FontSize(9);
                                        table.Cell().Element(CellStyle).Text("Giới tính:").FontSize(9);
                                        table.Cell().Element(CellStyle).Text(customerGender).FontSize(9);
                                        table.Cell().Element(CellStyle).Text("SDT:").FontSize(9);
                                        table.Cell().Element(CellStyle).Text(receipt.CustomerPhone ?? "-").FontSize(9);
                                        table.Cell().Element(CellStyle).Text("Địa chỉ:").FontSize(9);
                                        table.Cell().Element(CellStyle).Text(receipt.CustomerAddress ?? "-").FontSize(9);
                                        table.Cell().Element(CellStyle).Text("Mã khách hàng:").FontSize(9);
                                        table.Cell().Element(CellStyle).Text(receipt.CustomerCode ?? "-").FontSize(9);
                                    });
                                });

                                // Vehicle Info
                                row.RelativeItem().Column(vehicleCol =>
                                {
                                    vehicleCol.Item().PaddingBottom(5).Text("Thông tin xe").FontSize(11).Bold();
                                    vehicleCol.Item().Table(table =>
                                    {
                                        table.ColumnsDefinition(columns =>
                                        {
                                            columns.RelativeColumn(2);
                                            columns.RelativeColumn(3);
                                        });
                                        table.Cell().Element(CellStyle).Text("Tên xe:").FontSize(9);
                                        table.Cell().Element(CellStyle).Text(receipt.CarName ?? "-").FontSize(9);
                                        table.Cell().Element(CellStyle).Text("Năm sản xuất:").FontSize(9);
                                        table.Cell().Element(CellStyle).Text("-").FontSize(9);
                                        table.Cell().Element(CellStyle).Text("Biển số xe:").FontSize(9);
                                        table.Cell().Element(CellStyle).Text(receipt.LicensePlate ?? "-").FontSize(9);
                                        table.Cell().Element(CellStyle).Text("Loại xe:").FontSize(9);
                                        table.Cell().Element(CellStyle).Text(receipt.VehicleType ?? "-").FontSize(9);
                                        table.Cell().Element(CellStyle).Text("Màu sơn:").FontSize(9);
                                        table.Cell().Element(CellStyle).Text("-").FontSize(9);
                                        table.Cell().Element(CellStyle).Text("Số máy:").FontSize(9);
                                        table.Cell().Element(CellStyle).Text(receipt.EngineNumber ?? "-").FontSize(9);
                                        table.Cell().Element(CellStyle).Text("Số khung:").FontSize(9);
                                        table.Cell().Element(CellStyle).Text(receipt.VinNumber ?? "-").FontSize(9);
                                    });
                                });
                            });

                            column.Item().PaddingTop(10);

                            // Danh sách công việc
                            column.Item().PaddingBottom(5);
                            column.Item().Text("Danh sách công việc").FontSize(10).Bold();
                            column.Item().Table(tasksTable =>
                            {
                                tasksTable.ColumnsDefinition(columns =>
                                {
                                    columns.ConstantColumn(30); // STT
                                    columns.RelativeColumn(3);  // Tên công việc
                                    columns.ConstantColumn(100); // Phí nhân công
                                });

                                tasksTable.Header(header =>
                                {
                                    header.Cell().Element(HeaderCellStyle).AlignCenter().Text("STT").FontSize(9).Bold();
                                    header.Cell().Element(HeaderCellStyle).Text("Tên công việc").FontSize(9).Bold();
                                    header.Cell().Element(HeaderCellStyle).AlignRight().Text("Phí nhân công").FontSize(9).Bold();
                                });

                                int stt = 1;
                                foreach (var task in tasks)
                                {
                                    var laborCost = task.LaborCost ?? 0;
                                    tasksTable.Cell().Element(CellStyle).AlignCenter().Text(stt++.ToString()).FontSize(9);
                                    tasksTable.Cell().Element(CellStyle).Text(task.TaskName ?? "Công việc").FontSize(9);
                                    tasksTable.Cell().Element(CellStyle).AlignRight().Text(FormatCurrency(laborCost)).FontSize(9);
                                }
                            });

                            column.Item().PaddingTop(10);

                            // Phụ tùng sử dụng
                            column.Item().PaddingBottom(5);
                            column.Item().Text("Phụ tùng sử dụng").FontSize(10).Bold();
                            column.Item().Table(componentsTable =>
                            {
                                componentsTable.ColumnsDefinition(columns =>
                                {
                                    columns.ConstantColumn(30); // STT
                                    columns.RelativeColumn(3);  // Phụ tùng
                                    columns.ConstantColumn(80); // Số lượng
                                    columns.ConstantColumn(100);  // Đơn giá
                                    columns.ConstantColumn(120); // Thành tiền
                                });

                                componentsTable.Header(header =>
                                {
                                    header.Cell().Element(HeaderCellStyle).AlignCenter().Text("STT").FontSize(9).Bold();
                                    header.Cell().Element(HeaderCellStyle).Text("Phụ tùng").FontSize(9).Bold();
                                    header.Cell().Element(HeaderCellStyle).AlignCenter().Text("Số lượng").FontSize(9).Bold();
                                    header.Cell().Element(HeaderCellStyle).AlignRight().Text("Đơn giá").FontSize(9).Bold();
                                    header.Cell().Element(HeaderCellStyle).AlignRight().Text("Thành tiền").FontSize(9).Bold();
                                });

                                int stt = 1;
                                foreach (var component in components)
                                {
                                    var quantity = component.ActualQuantity ?? (decimal)component.Quantity;
                                    var unitPrice = component.UnitPrice ?? 0;
                                    var total = component.TotalPrice ?? (quantity * unitPrice);

                                    componentsTable.Cell().Element(CellStyle).AlignCenter().Text(stt++.ToString()).FontSize(9);
                                    var componentName = component.ComponentName ?? "Phụ tùng";
                                    if (!string.IsNullOrEmpty(component.ComponentCode))
                                    {
                                        componentName += $" (Mã: {component.ComponentCode})";
                                    }
                                    componentsTable.Cell().Element(CellStyle).Text(componentName).FontSize(9);
                                    componentsTable.Cell().Element(CellStyle).AlignCenter().Text(quantity.ToString("F2")).FontSize(9);
                                    componentsTable.Cell().Element(CellStyle).AlignRight().Text(FormatCurrency(unitPrice)).FontSize(9);
                                    componentsTable.Cell().Element(CellStyle).AlignRight().Text(FormatCurrency(total)).FontSize(9);
                                }
                            });

                            // Tổng hợp chi phí (giống như trên web)
                            column.Item().PaddingTop(15);
                            
                            var laborTotal = tasks.Sum(t => t.LaborCost ?? 0);
                            var componentTotal = components.Sum(c => 
                            {
                                var qty = c.ActualQuantity ?? (decimal)c.Quantity;
                                var price = c.UnitPrice ?? 0;
                                return c.TotalPrice ?? (qty * price);
                            });
                            
                            var totalBeforeTax = receipt.Subtotal ?? (laborTotal + componentTotal);
                            var surcharge = receipt.SurchargeAmount ?? 0;
                            var discount = receipt.DiscountAmount ?? 0;
                            var packageDiscount = receipt.PackageDiscountAmount ?? 0;
                            var packageId = receipt.ServicePackageId;
                            var packageName = receipt.ServicePackageName;
                            
                            // Bước 1: Tính tổng discount = package discount + discount khác
                            var totalDiscount = discount + packageDiscount;
                            
                            // Bước 2: Trừ tất cả giảm giá
                            var amountAfterDiscount = Math.Max(0, totalBeforeTax - totalDiscount);
                            
                            // Bước 3: Tính VAT trên số tiền sau khi đã trừ giảm giá (KHÔNG tính trên phụ thu)
                            var vatPercent = receipt.VatPercent ?? 10;
                            var totalVat = vatPercent * amountAfterDiscount / 100;
                            
                            // Grand total = (Subtotal - TotalDiscount) + VAT + Surcharge
                            // Luôn tính lại để đảm bảo chính xác, không dùng receipt.FinalAmount
                            var totalPayment = amountAfterDiscount + totalVat + surcharge;

                            column.Item().Table(summaryTable =>
                            {
                                summaryTable.ColumnsDefinition(columns =>
                                {
                                    columns.RelativeColumn(2);
                                    columns.ConstantColumn(150);
                                });

                                summaryTable.Cell().Element(CellStyle).Text("Phí nhân công").FontSize(9);
                                summaryTable.Cell().Element(CellStyle).AlignRight().Text(FormatCurrency(laborTotal)).FontSize(9);

                                summaryTable.Cell().Element(CellStyle).Text("Chi phí phụ tùng").FontSize(9);
                                summaryTable.Cell().Element(CellStyle).AlignRight().Text(FormatCurrency(componentTotal)).FontSize(9);

                                summaryTable.Cell().Element(CellStyle).Text("Tổng phụ (Subtotal)").FontSize(9).Bold();
                                summaryTable.Cell().Element(CellStyle).AlignRight().Text(FormatCurrency(totalBeforeTax)).FontSize(9).Bold();

                                summaryTable.Cell().Element(CellStyle).Text($"Thuế VAT ({vatPercent:F1}%)").FontSize(9);
                                summaryTable.Cell().Element(CellStyle).AlignRight().Text(FormatCurrency(totalVat)).FontSize(9);

                                summaryTable.Cell().Element(CellStyle).Text("Phụ thu").FontSize(9);
                                summaryTable.Cell().Element(CellStyle).AlignRight().Text(FormatCurrency(surcharge)).FontSize(9);

                                // Hiển thị giảm giá gói dịch vụ nếu có packageId
                                if (packageId.HasValue)
                                {
                                    var displayName = packageName ?? "Gói dịch vụ";
                                    summaryTable.Cell().Element(CellStyle).Text($"Giảm giá gói dịch vụ ({displayName})").FontSize(9);
                                    summaryTable.Cell().Element(CellStyle).AlignRight().Text($"-{FormatCurrency(packageDiscount)}").FontSize(9);
                                }

                                summaryTable.Cell().Element(CellStyle).Text("Giảm giá").FontSize(9);
                                summaryTable.Cell().Element(CellStyle).AlignRight().Text($"-{FormatCurrency(discount)}").FontSize(9);

                                summaryTable.Cell().Element(HeaderCellStyle).Text("Tổng thanh toán").FontSize(9).Bold();
                                summaryTable.Cell().Element(HeaderCellStyle).AlignRight().Text(FormatCurrency(totalPayment)).FontSize(9).Bold();
                            });

                            column.Item().PaddingTop(10);

                            // Amount in words
                            column.Item().Text($"Số tiền viết bằng chữ: {ConvertNumberToWords(totalPayment)}").FontSize(9);

                            column.Item().PaddingTop(20);

                            // Signatures
                            column.Item().Row(row =>
                            {
                                row.RelativeItem().Column(col =>
                                {
                                    col.Item().AlignCenter().Text("Người mua hàng").FontSize(9).Bold();
                                    col.Item().AlignCenter().Text("(Ký, họ tên)").FontSize(8).Italic();
                                });
                                row.RelativeItem().Column(col =>
                                {
                                    col.Item().AlignCenter().Text("Kế toán trưởng").FontSize(9).Bold();
                                    col.Item().AlignCenter().Text("(Ký, họ tên)").FontSize(8).Italic();
                                });
                                row.RelativeItem().Column(col =>
                                {
                                    col.Item().AlignCenter().Text("Giám đốc").FontSize(9).Bold();
                                    col.Item().AlignCenter().Text("(Ký, họ tên, đóng dấu)").FontSize(8).Italic();
                                    col.Item().PaddingTop(5).AlignCenter().Text("Ngày ..... tháng .... năm.....").FontSize(8);
                                });
                            });
                        });
                });
            });

                return document.GeneratePdf();
            }
            catch (Exception)
            {
                throw;
            }
        }

        private static IContainer HeaderCellStyle(IContainer container)
        {
            return container
                .BorderBottom(0.5f)
                .BorderColor(Colors.Grey.Darken1)
                .Background(Colors.Grey.Lighten4)
                .Padding(5)
                .AlignCenter();
        }

        private static string FormatCurrency(decimal amount)
        {
            return $"{amount:N0} ₫";
        }

        private static string ConvertNumberToWords(decimal amount)
        {
            var ones = new[] { "", "một", "hai", "ba", "bốn", "năm", "sáu", "bảy", "tám", "chín" };
            var tens = new[] { "", "mười", "hai mươi", "ba mươi", "bốn mươi", "năm mươi", "sáu mươi", "bảy mươi", "tám mươi", "chín mươi" };
            var hundreds = new[] { "", "một trăm", "hai trăm", "ba trăm", "bốn trăm", "năm trăm", "sáu trăm", "bảy trăm", "tám trăm", "chín trăm" };

            if (amount == 0) return "không";

            var total = (long)amount;
            var result = "";

            var millions = total / 1000000;
            var thousands = (total % 1000000) / 1000;
            var remainder = total % 1000;

            if (millions > 0)
            {
                result += ConvertThreeDigits(millions, hundreds, tens, ones) + " triệu ";
            }
            if (thousands > 0)
            {
                result += ConvertThreeDigits(thousands, hundreds, tens, ones) + " nghìn ";
            }
            if (remainder > 0 || result == "")
            {
                result += ConvertThreeDigits(remainder, hundreds, tens, ones);
            }

            return result.Trim() + " đồng";
        }

        private static string ConvertThreeDigits(long num, string[] hundreds, string[] tens, string[] ones)
        {
            if (num == 0) return "";
            var result = "";
            var h = (int)(num / 100);
            var t = (int)((num % 100) / 10);
            var o = (int)(num % 10);

            if (h > 0)
            {
                result += hundreds[h] + " ";
            }
            if (t > 1)
            {
                result += tens[t];
                if (o > 0)
                {
                    result += " " + ones[o];
                }
            }
            else if (t == 1)
            {
                result += "mười";
                if (o > 0)
                {
                    if (o == 5) result += " lăm";
                    else result += " " + ones[o];
                }
            }
            else if (o > 0)
            {
                if (h > 0 && o == 5) result += "lăm";
                else result += ones[o];
            }
            return result.Trim();
        }
    }
}
