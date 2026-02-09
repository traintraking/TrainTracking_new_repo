using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using TrainTracking.Domain.Entities;
using QRCoder;
using System.IO;
using Microsoft.AspNetCore.Hosting;
using SkiaSharp;

namespace TrainTracking.Web.Services
{
    public class TicketGenerator
    {
        private readonly IWebHostEnvironment _webHostEnvironment;

        public TicketGenerator(IWebHostEnvironment webHostEnvironment)
        {
            _webHostEnvironment = webHostEnvironment;
        }

        public byte[] GenerateMultipleTicketsPdf(List<Booking> bookings, string baseUrl, Dictionary<string, string> userNationalIds)
        {
            string logoPath = Path.Combine(_webHostEnvironment.WebRootPath, "images", "logo1.png");
            byte[]? logoImage = File.Exists(logoPath) ? File.ReadAllBytes(logoPath) : null;

            var document = QuestPDF.Fluent.Document.Create(container =>
            {
                foreach (var booking in bookings)
                {
                    string qrUrl = $"{baseUrl}/Bookings/TicketDetails/{booking.Id}";
                    byte[] qrCodeImage = GenerateQrCode(qrUrl);
                    string bookingIdShort = booking.Id.ToString().Substring(0, 8).ToUpper();
                    string nationalId = userNationalIds.ContainsKey(booking.UserId) ? userNationalIds[booking.UserId] : "";

                    container.Page(page =>
                    {
                        page.Size(PageSizes.A5.Landscape());
                        page.Margin(0);
                        page.PageColor(Colors.White);
                        
                        // Set global text style - attempting to use a standard font that supports some Arabic if possible, 
                        // though QuestPDF default fonts might have limits. Fallback to common sans-serif.
                        page.DefaultTextStyle(x => x.FontSize(10).FontFamily(Fonts.Arial));

                        page.Content().Element(content =>
                        {
                            // Use minimal column to ensure content fits
                            content.Column(col =>
                            {
                                // --- Header Section ---
                                // Reduced height to 80
                                col.Item().Height(80).Background("#1a3c6e").PaddingHorizontal(20).Row(row =>
                                {
                                    // 1. Logo (Left)
                                    if (logoImage != null)
                                    {
                                        row.ConstantItem(60).AlignMiddle().Image(logoImage).FitArea();
                                    }
                                    else
                                    {
                                        row.ConstantItem(60).AlignMiddle().Text("KUW").FontSize(22).Bold().FontColor(Colors.White);
                                    }
                                    
                                    // 2. Center Text "OFFICIAL E-TICKET"
                                    row.RelativeItem().AlignMiddle().AlignCenter().Text("O    F    F    I    C    I    A    L     E    -    T    I    C    K    E    T").FontSize(12).Medium().FontColor(Colors.White);

                                    // 3. Brand Name (Right) "KuwGo"
                                    row.ConstantItem(100).AlignMiddle().AlignRight().Text("Sikka").FontSize(24).Bold().FontColor(Colors.White);
                                });

                                // --- Main Body ---
                                // Restored padding to 40 for better spacing
                                col.Item().Padding(40).Row(mainRow =>
                                {
                                    // Left Side: Data
                                    mainRow.RelativeItem(2).Column(details =>
                                    {
                                        // Row 1: Date & Passenger
                                        details.Item().Row(r =>
                                        {
                                            // Date (Left)
                                            r.RelativeItem().Column(c =>
                                            {
                                                c.Item().Text(t => {
                                                    t.Span("Date / ").FontSize(9).FontColor(Colors.Grey.Medium);
                                                    t.Span("التاريخ").FontSize(9).FontColor(Colors.Grey.Medium);
                                                });
                                                c.Item().Text(booking.Trip.DepartureTime.ToString("yyyy-MM-dd")).FontSize(14).Bold().FontColor("#1a3c6e");
                                            });

                                            // Passenger (Right)
                                            r.RelativeItem().AlignRight().Column(c =>
                                            {
                                                c.Item().AlignRight().Text(t => {
                                                    t.Span("Passenger / ").FontSize(9).FontColor(Colors.Grey.Medium);
                                                    t.Span("المسافر").FontSize(9).FontColor(Colors.Grey.Medium);
                                                });
                                                c.Item().AlignRight().Text(booking.PassengerName ?? "Unknown").FontSize(14).Bold().FontColor("#1a3c6e");
                                                
                                                if (!string.IsNullOrEmpty(nationalId))
                                                {
                                                    c.Item().AlignRight().Text($"ID: {nationalId}").FontSize(9).FontColor(Colors.Grey.Darken2);
                                                }
                                            });
                                        });

                                        // Restored vertical padding
                                        details.Item().PaddingVertical(20).LineHorizontal(1).LineColor(Colors.Grey.Lighten4);

                                        // Row 2: To & From
                                        details.Item().Row(r =>
                                        {
                                            // To (Left)
                                            r.RelativeItem(2).Column(c =>
                                            {
                                                c.Item().Text(t => {
                                                    t.Span("To / ").FontSize(9).FontColor(Colors.Grey.Medium);
                                                    t.Span("إلى").FontSize(9).FontColor(Colors.Grey.Medium);
                                                });
                                                c.Item().Text(booking.Trip.ToStation?.Name ?? "Unknown").FontSize(16).Bold().FontColor("#e74c3c");
                                                c.Item().Text(booking.Trip.ArrivalTime.ToString("HH:mm")).FontSize(12);
                                            });

                                            // Arrow (Center)
                                            r.RelativeItem(1).AlignMiddle().AlignCenter().Text("<---").FontSize(14).Bold().FontColor(Colors.Grey.Medium);

                                            // From (Right)
                                            r.RelativeItem(2).AlignRight().Column(c =>
                                            {
                                                c.Item().AlignRight().Text(t => {
                                                    t.Span("From / ").FontSize(9).FontColor(Colors.Grey.Medium);
                                                    t.Span("من").FontSize(9).FontColor(Colors.Grey.Medium);
                                                });
                                                c.Item().AlignRight().Text(booking.Trip.FromStation?.Name ?? "Unknown").FontSize(16).Bold().FontColor("#e74c3c");
                                                c.Item().AlignRight().Text(booking.Trip.DepartureTime.ToString("HH:mm")).FontSize(12);
                                            });
                                        });

                                        // Restored vertical padding
                                        details.Item().PaddingVertical(20).LineHorizontal(1).LineColor(Colors.Grey.Lighten4);

                                        // Row 3: Price, Seat, Carriage, Train
                                        details.Item().Row(r =>
                                        {
                                            // Price
                                            r.RelativeItem().Column(c =>
                                            {
                                                c.Item().AlignCenter().Text(t => {
                                                    t.Span("Price / ").FontSize(8).FontColor(Colors.Grey.Medium);
                                                    t.Span("السعر").FontSize(8).FontColor(Colors.Grey.Medium);
                                                });
                                                c.Item().AlignCenter().Text($"EGP {booking.Price:F3}").FontSize(12).Bold();
                                            });
                                            // Seat
                                            r.RelativeItem().Column(c =>
                                            {
                                                c.Item().AlignCenter().Text(t => {
                                                    t.Span("Seat / ").FontSize(8).FontColor(Colors.Grey.Medium);
                                                    t.Span("المقعد").FontSize(8).FontColor(Colors.Grey.Medium);
                                                });
                                                c.Item().AlignCenter().Text(booking.SeatNumber.ToString()).FontSize(12).Bold();
                                            });
                                            // Carriage (New)
                                            r.RelativeItem().Column(c =>
                                            {
                                                // Simple logic for Carriage: assuming 60 seats per carriage. 
                                                int carriageNum = ((booking.SeatNumber - 1) / 20) + 1;
                                                c.Item().AlignCenter().Text(t => {
                                                    t.Span("Carriage / ").FontSize(8).FontColor(Colors.Grey.Medium);
                                                    t.Span("عربة").FontSize(8).FontColor(Colors.Grey.Medium);
                                                });
                                                c.Item().AlignCenter().Text(carriageNum.ToString()).FontSize(12).Bold();
                                            });
                                            // Train
                                            r.RelativeItem().Column(c =>
                                            {
                                                c.Item().AlignCenter().Text(t => {
                                                    t.Span("Train / ").FontSize(8).FontColor(Colors.Grey.Medium);
                                                    t.Span("القطار").FontSize(8).FontColor(Colors.Grey.Medium);
                                                });
                                                c.Item().AlignCenter().Text(booking.Trip.Train?.TrainNumber ?? "N/A").FontSize(12).Bold();
                                            });
                                        });
                                    });

                                    // Right Side: QR Code
                                    // Add a dashed border to the left of this column
                                    mainRow.RelativeItem(1).PaddingLeft(15).BorderLeft(1).BorderColor(Colors.Grey.Lighten2).Column(qrCol =>
                                    {
                                        qrCol.Item().AlignCenter().PaddingTop(10).Width(100).Image(qrCodeImage);
                                        qrCol.Item().PaddingTop(5).AlignCenter().Text($"ID: {bookingIdShort}").FontSize(9).Bold();
                                        qrCol.Item().AlignCenter().Text("Scan to verify").FontSize(6).FontColor(Colors.Grey.Medium);
                                    });
                                });
                            });
                        });
                        
                        page.Footer().Column(col => 
                        {
                             // --- Footer ---
                            col.Item().Background(Colors.Grey.Lighten5).Padding(10).AlignCenter().Text(t =>
                            {
                                t.Span("Have a safe trip - ").Italic().FontColor(Colors.Grey.Medium);
                                t.Span("نتمنى لكم رحلة آمنة وسعيدة"); 
                            });
                            
                            // Bottom Green Bar
                            col.Item().Height(10).Background("#2ecc71");
                        });
                    });
                }
            });

            return document.GeneratePdf();
        }

        private byte[] GenerateQrCode(string url)
        {
            using (QRCodeGenerator qrGenerator = new QRCodeGenerator())
            {
                QRCodeData qrCodeData = qrGenerator.CreateQrCode(url, QRCodeGenerator.ECCLevel.Q);
                using (PngByteQRCode qrCode = new PngByteQRCode(qrCodeData))
                {
                    return qrCode.GetGraphic(20);
                }
            }
        }
    }
}
