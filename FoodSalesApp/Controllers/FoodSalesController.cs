using Microsoft.AspNetCore.Mvc;
using OfficeOpenXml;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace FoodSalesApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class FoodSalesController : ControllerBase
    {
        private readonly string filePath = "../Food sales.xlsx";

        // Model สำหรับข้อมูลการขาย
        public class FoodSale
        {
            public int Id { get; set; }
            public DateTime Date { get; set; }
            public string Item { get; set; } = string.Empty;

            public int Quantity { get; set; }
            public decimal Price { get; set; }
        }

        // ฟังก์ชันเพื่อดึงข้อมูลทั้งหมดจากไฟล์ Excel
        [HttpGet]
        public IActionResult GetSalesData(string sortColumn = "Date", bool ascending = true, DateTime? startDate = null, DateTime? endDate = null, string search = "")
        {
            var salesData = LoadDataFromExcel();

            // กรองข้อมูลตามช่วงวันที่
            if (startDate.HasValue && endDate.HasValue)
            {
                salesData = salesData.Where(s => s.Date >= startDate.Value && s.Date <= endDate.Value).ToList();
            }

            // ค้นหาข้อมูลตามคำค้น
            if (!string.IsNullOrEmpty(search))
            {
                salesData = salesData.Where(s => s.Item.Contains(search, StringComparison.OrdinalIgnoreCase)).ToList();
            }

            // เรียงลำดับข้อมูลตามคอลัมน์ที่กำหนด
            salesData = sortColumn switch
            {
                "Item" => ascending ? salesData.OrderBy(s => s.Item).ToList() : salesData.OrderByDescending(s => s.Item).ToList(),
                "Quantity" => ascending ? salesData.OrderBy(s => s.Quantity).ToList() : salesData.OrderByDescending(s => s.Quantity).ToList(),
                "Price" => ascending ? salesData.OrderBy(s => s.Price).ToList() : salesData.OrderByDescending(s => s.Price).ToList(),
                _ => ascending ? salesData.OrderBy(s => s.Date).ToList() : salesData.OrderByDescending(s => s.Date).ToList(),
            };

            return Ok(salesData);
        }

        // เพิ่มข้อมูลการขายใหม่
        [HttpPost]
        public IActionResult AddSale(FoodSale newSale)
        {
            var salesData = LoadDataFromExcel();
            newSale.Id = salesData.Max(s => s.Id) + 1;
            salesData.Add(newSale);
            SaveDataToExcel(salesData);
            return Ok(newSale);
        }

        // แก้ไขข้อมูลการขาย
        [HttpPut("{id}")]
        public IActionResult UpdateSale(int id, FoodSale updatedSale)
        {
            var salesData = LoadDataFromExcel();
            var sale = salesData.FirstOrDefault(s => s.Id == id);
            if (sale == null) return NotFound();

            sale.Date = updatedSale.Date;
            sale.Item = updatedSale.Item;
            sale.Quantity = updatedSale.Quantity;
            sale.Price = updatedSale.Price;

            SaveDataToExcel(salesData);
            return Ok(sale);
        }

        // ลบข้อมูลการขาย
        [HttpDelete("{id}")]
        public IActionResult DeleteSale(int id)
        {
            var salesData = LoadDataFromExcel();
            var sale = salesData.FirstOrDefault(s => s.Id == id);
            if (sale == null) return NotFound();

            salesData.Remove(sale);
            SaveDataToExcel(salesData);
            return NoContent();
        }

        // ฟังก์ชันเพื่อโหลดข้อมูลจาก Excel
        private List<FoodSale> LoadDataFromExcel()
        {
            var salesData = new List<FoodSale>();
            using (var package = new ExcelPackage(new FileInfo(filePath)))
            {
                var worksheet = package.Workbook.Worksheets[0];
                for (int row = 2; row <= worksheet.Dimension.End.Row; row++)
                {
                    salesData.Add(new FoodSale
                    {
                        Id = int.Parse(worksheet.Cells[row, 1].Text),
                        Date = DateTime.Parse(worksheet.Cells[row, 2].Text),
                        Item = worksheet.Cells[row, 3].Text,
                        Quantity = int.Parse(worksheet.Cells[row, 4].Text),
                        Price = decimal.Parse(worksheet.Cells[row, 5].Text)
                    });
                }
            }
            return salesData;
        }

        // ฟังก์ชันเพื่อบันทึกข้อมูลกลับไปที่ Excel
        private void SaveDataToExcel(List<FoodSale> salesData)
        {
            using (var package = new ExcelPackage(new FileInfo(filePath)))
            {
                var worksheet = package.Workbook.Worksheets[0];
                worksheet.Cells.Clear();  // ล้างข้อมูลเก่าออก

                // เขียนหัวคอลัมน์
                worksheet.Cells[1, 1].Value = "Id";
                worksheet.Cells[1, 2].Value = "Date";
                worksheet.Cells[1, 3].Value = "Item";
                worksheet.Cells[1, 4].Value = "Quantity";
                worksheet.Cells[1, 5].Value = "Price";

                // เขียนข้อมูลใหม่
                int row = 2;
                foreach (var sale in salesData)
                {
                    worksheet.Cells[row, 1].Value = sale.Id;
                    worksheet.Cells[row, 2].Value = sale.Date;
                    worksheet.Cells[row, 3].Value = sale.Item;
                    worksheet.Cells[row, 4].Value = sale.Quantity;
                    worksheet.Cells[row, 5].Value = sale.Price;
                    row++;
                }
                package.Save();
            }
        }
    }
}
