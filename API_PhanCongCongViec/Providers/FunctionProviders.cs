using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;

namespace API_Tracy.Providers
{
    public static class FunctionProviders
    {
        public static string[] FindItemNotExist(string[] arrA, string[] arrB)
        {
            List<string> result = new List<string>();
            foreach (var item in arrA)
            {
                if (!arrB.Contains(item))
                    result.Add(item);
            }
            return result.ToArray();
        }

        public static string TinhThamNien(this Nullable<DateTime> date)
        {
            if (date == null)
                return "";

            double TongNgay = DateTime.Now.Subtract(date ?? DateTime.Now).TotalDays;

            int sonam = (int)(TongNgay / 365);
            int sothang = (int)((TongNgay % 365) / 30);
            int songay = (int)((TongNgay - 365 * sonam - 30 * sothang));
            return (sonam + " năm, " + sothang + " tháng, " + songay + " ngày - (tổng " + TongNgay.ToString("#,###") + " ngày)");
        }
        public static string ConvertImageToBase64(string Path)
        {
            if (!File.Exists(Path))
                return "";

            using (Image image = Image.FromFile(Path))
            {
                using (MemoryStream m = new MemoryStream())
                {
                    image.Save(m, image.RawFormat);
                    byte[] imageBytes = m.ToArray();

                    // Convert byte[] to Base64 String
                    string base64String = Convert.ToBase64String(imageBytes);
                    return base64String;
                }
            }
        }
        public static double KiemTraKhongNhap_LoadLen(this string SoTien)
        {
            double KQ = 0;
            try
            {
                KQ = double.Parse(SoTien);
            }
            catch { }
            return KQ;
        }

        public static string GetExtensionFileFromBase64(string base64String)
        {
            string[] strings = base64String.Split(';');
            string extension;
            switch (strings[0])
            {//check image's extension
                case "data:image/jpeg":
                    extension = "jpeg";
                    break;
                case "data:image/png":
                    extension = "png";
                    break;
                default://should write cases for more images types
                    extension = "jpg";
                    break;
            }
            return extension;
        }
        public static Image ConvertBase64ToImage(string base64String)
        {
            // Convert base 64 string to byte[]
            byte[] imageBytes = Convert.FromBase64String(base64String);
            // Convert byte[] to Image
            using (var ms = new MemoryStream(imageBytes, 0, imageBytes.Length))
            {
                Image image = Image.FromStream(ms, true);
                return image;
            }
        }

        #region CONVERT DECIMAL TO STRING DOLLAR

        public static string ConvertDecimalToString_English(this string numb, bool isCurrency)
        {
            string val = NumberToWordConverter.ConverterToCurrency(double.Parse(numb), "Dollar", "Cent");
            return val;
        }
        #endregion

        #region CONVERT DECIMAL TO STRING VND
        public static string ConvertDecimalToString_VietNamese(this string number, bool isCurrency)
        {
            if (!number.Contains("."))
                number += ".00";

            string s = number.Split('.')[0].Replace(",", "");
            string[] so = new string[] { "không", "một", "hai", "ba", "bốn", "năm", "sáu", "bảy", "tám", "chín" };
            string[] hang = new string[] { "", "nghìn", "triệu", "tỷ" };
            int i, j, donvi, chuc, tram;
            string str = " ";
            bool booAm = false;
            decimal decS = 0;
            //Tung addnew
            try
            {
                decS = Convert.ToDecimal(s.ToString());
            }
            catch
            {
            }
            if (decS < 0)
            {
                decS = -decS;
                s = decS.ToString();
                booAm = true;
            }
            i = s.Length;
            if (i == 0)
                str = so[0] + str;
            else
            {
                j = 0;
                while (i > 0)
                {
                    donvi = Convert.ToInt32(s.Substring(i - 1, 1));
                    i--;
                    if (i > 0)
                        chuc = Convert.ToInt32(s.Substring(i - 1, 1));
                    else
                        chuc = -1;
                    i--;
                    if (i > 0)
                        tram = Convert.ToInt32(s.Substring(i - 1, 1));
                    else
                        tram = -1;
                    i--;
                    if ((donvi > 0) || (chuc > 0) || (tram > 0) || (j == 3))
                        str = hang[j] + str;
                    j++;
                    if (j > 3) j = 1;
                    if ((donvi == 1) && (chuc > 1))
                        str = "một " + str;
                    else
                    {
                        if ((donvi == 5) && (chuc > 0))
                            str = "lăm " + str;
                        else if (donvi > 0)
                            str = so[donvi] + " " + str;
                    }
                    if (chuc < 0)
                        break;
                    else
                    {
                        if ((chuc == 0) && (donvi > 0)) str = "lẻ " + str;
                        if (chuc == 1) str = "mười " + str;
                        if (chuc > 1) str = so[chuc] + " mươi " + str;
                    }
                    if (tram < 0) break;
                    else
                    {
                        if ((tram > 0) || (chuc > 0) || (donvi > 0)) str = so[tram] + " trăm " + str;
                    }
                    str = " " + str;
                }
            }
            if (booAm) str = "Âm " + str;
            return (str + "đô mỹ và " + (isCurrency ? (CountCents_VietNamese(number.Split('.')[1].Replace(",", ""))) : "")).Replace("và  cent", "");
        }
        private static string CountCents_VietNamese(string number)
        {
            number = KiemTraKhongNhap_LoadLen(number).ToString();
            string cents = ConvertDecimalToString_VietNamese(number, false).Replace("đô mỹ và", "cent");
            return cents;
        }
        #endregion

        #region CONVERT DATETIME

        public static string ConvertDDMMtoMMDD(this string ngay)
        {
            if (ngay.Equals(""))
            {
                return "";
            }
            else
            {
                string ngayC = ngay.Substring(0, 2);
                string thangC = ngay.Substring(3, 2);
                string namC = ngay.Substring(6, 4);
                return thangC + "/" + ngayC + "/" + namC;
            }
        }
        public static string ConvertMMDDYYtoDDMMYY(this string ngay)
        {
            try
            {
                if (ngay.Trim() == "")
                {
                    return "";
                }
                else
                {
                    int ngayC = 0;
                    int thangC = 0;
                    int namC = 0;
                    try
                    {
                        thangC = int.Parse(ngay.Substring(0, 2));
                        try
                        {
                            ngayC = int.Parse(ngay.Substring(3, 2));
                            namC = int.Parse(ngay.Substring(6, 4));
                        }
                        catch
                        {
                            ngayC = int.Parse(ngay.Substring(3, 1));
                            namC = int.Parse(ngay.Substring(5, 4));
                        }
                    }
                    catch
                    {
                        thangC = int.Parse(ngay.Substring(0, 1));
                        try
                        {
                            ngayC = int.Parse(ngay.Substring(2, 2));
                            namC = int.Parse(ngay.Substring(5, 4));
                        }
                        catch
                        {
                            ngayC = int.Parse(ngay.Substring(2, 1));
                            namC = int.Parse(ngay.Substring(4, 4));
                        }
                    }
                    string ngaytrave = "";
                    if (ngayC < 10 && thangC < 10)
                        ngaytrave = "0" + ngayC.ToString() + "/0" + thangC.ToString() + "/" + namC.ToString();
                    if (ngayC < 10 && thangC >= 10)
                        ngaytrave = "0" + ngayC.ToString() + "/" + thangC.ToString() + "/" + namC.ToString();
                    if (ngayC >= 10 && thangC < 10)
                        ngaytrave = ngayC.ToString() + "/0" + thangC.ToString() + "/" + namC.ToString();
                    if (ngayC >= 10 && thangC >= 10)
                        ngaytrave = ngayC.ToString() + "/" + thangC.ToString() + "/" + namC.ToString();

                    return ngaytrave;
                }
            }
            catch
            {
                return "";
            }
        }
        #endregion
    }
}