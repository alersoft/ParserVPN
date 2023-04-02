// Импорт необходимых библиотек
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Windows.Forms;

// Объявление пространства имен и класса
namespace VPNGateParser
{
    public partial class Form1 : Form
    {
        // Словарь для хранения названий стран и их кодов
        private readonly Dictionary<string, string> _countries = new Dictionary<string, string>()
        {
            { "All countries", null }, // Все страны
            { "United States", "US" }, // США
            { "Japan", "JP" }, // Япония
            { "Korea", "KR" }, // Корея
            { "Guam", "GU" }, // Гуам
            { "Indonesia", "ID" }, // Индонезия
            { "India", "IN" }, // Индия
            { "Viet Nam", "VN" }, // Вьетнам
            { "Thailand", "TH" }, // Таиланд
            { "China", "CN" }, // Китай
            { "Russia", "RU" } // Россия
        };

        // Папка для сохранения конфигурационных файлов OpenVPN
        private readonly string _outputFolder = "OpenVpnFile";

        // Конструктор класса
        public Form1()
        {
            // Инициализация компонентов формы
            InitializeComponent();

            // Настройка выпадающего списка для выбора страны
            countryComboBox.DisplayMember = "Key";
            countryComboBox.ValueMember = "Value";
            countryComboBox.DataSource = new BindingSource(_countries, null);

            // Создание папки для сохранения конфигурационных файлов, если ее нет
            if (!Directory.Exists(_outputFolder))
            {
                Directory.CreateDirectory(_outputFolder);
            }

            // Добавление столбцов в таблицу для отображения результатов поиска
            dataGridView1.Columns.Add("HostName", "HostName"); // Имя хоста
            dataGridView1.Columns.Add("IP", "IP"); // IP-адрес
            dataGridView1.Columns.Add("Speed", "Speed"); // Скорость
            dataGridView1.Columns.Add("Ping", "Ping"); // Задержка
            dataGridView1.Columns.Add("Country", "Country"); // Страна
            dataGridView1.Columns.Add("CountryShort", "CountryShort"); // Код страны
            dataGridView1.Columns["CountryShort"].Visible = false; // Скрыть столбец CountryShort
            dataGridView1.Columns.Add("OpenVPNConfig", "OpenVPNConfig"); // Конфигурационный файл OpenVPN
            dataGridView1.Columns["OpenVPNConfig"].Visible = false; // Скрыть столбец OpenVPNConfig
        }

        // Обработчик события нажатия на кнопку "Найти"
        private void button1_Click(object sender, EventArgs e)
        {
            dataGridView1.Rows.Clear(); // Очистка строк в таблице

            string url = "http://www.vpngate.net/api/iphone/"; // URL для получения данных

            string[] list = GetHttpResponse(url).Split('\n'); // Получение ответа от сервера

            list = Array.FindAll(list, l => !l.StartsWith("#HostName")); // Отбрасывание комментариев в списке

            int i = 1;

            foreach (string vpn in list) // Перебор всех vpn-серверов в списке
            {
                string[] vpnData = vpn.Split(','); // Разделение строки на массив данных

                if (
                    vpnData.Length > 14
                    && !string.IsNullOrEmpty(vpnData[0])
                    && !string.IsNullOrEmpty(vpnData[6])
                )
                {
                    // Проверка, соответствует ли страна фильтру
                    if (
                        countryComboBox.SelectedValue == null
                        || vpnData[6].EndsWith(
                            countryComboBox.SelectedValue.ToString(),
                            StringComparison.OrdinalIgnoreCase
                        )
                    )
                    {
                        // Добавление строки в таблицу
                        dataGridView1.Rows.Add(
                            vpnData[0],
                            vpnData[1],
                            vpnData[2],
                            vpnData[3],
                            vpnData[5],
                            vpnData[6],
                            vpnData[14]
                        );
                        i++;
                    }
                }
            }
        }

        static string GetHttpResponse(string url, string data = null)
        {
            // Создаем объект HttpWebRequest для отправки запроса по указанному URL
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            // Устанавливаем метод запроса (GET по умолчанию)
            request.Method = "GET";
            // Если переданы данные для отправки (data), то меняем метод запроса на POST
            if (!string.IsNullOrEmpty(data))
            {
                request.Method = "POST";
                // Преобразуем строку с данными в массив байтов
                byte[] postData = System.Text.Encoding.UTF8.GetBytes(data);
                // Устанавливаем тип контента в заголовке запроса
                request.ContentType = "application/x-www-form-urlencoded";
                // Устанавливаем длину данных в байтах
                request.ContentLength = postData.Length;
                // Получаем поток для записи данных в запрос
                using (Stream stream = request.GetRequestStream())
                {
                    // Записываем данные в поток
                    stream.Write(postData, 0, postData.Length);
                }
            }

            // Отправляем запрос и получаем ответ
            using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
            {
                // Получаем поток для чтения данных из ответа
                using (Stream responseStream = response.GetResponseStream())
                {
                    // Создаем объект StreamReader для чтения данных из потока
                    using (StreamReader reader = new StreamReader(responseStream))
                    {
                        // Читаем все данные из потока и возвращаем их как строку
                        return reader.ReadToEnd();
                    }
                }
            }
        }

        // Метод для сохранения данных в файл в формате Base64
        static void SaveBase64ToFile(string base64String, string fileName)
        {
            // Преобразуем строку в формате Base64 в массив байтов
            byte[] bytes = Convert.FromBase64String(base64String);
            // Сохраняем массив байтов в файл
            File.WriteAllBytes(fileName, bytes);
        }

        private void dataGridView1_CellMouseClick(object sender, DataGridViewCellMouseEventArgs e)
        {
            // Проверяем, что была нажата правая кнопка мыши и номер строки в таблице неотрицательный
            if (e.Button == MouseButtons.Right && e.RowIndex >= 0)
            {
                // Получаем выделенную строку таблицы
                var selectedRow = dataGridView1.Rows[e.RowIndex];
                // Получаем значения столбцов IP и CountryShort для выделенной строки
                var IP = selectedRow.Cells["IP"].Value.ToString();
                var CountryShort = selectedRow.Cells["CountryShort"].Value.ToString();
                // Формируем имя файла, используя значения IP и CountryShort
                var fileName = $"{_outputFolder}\\{IP}_{CountryShort}.ovpn";
                // Получаем данные в формате Base64 из столбца OpenVPNConfig для выделенной строки
                var base64data = selectedRow.Cells["OpenVPNConfig"].Value.ToString();
                // Сохраняем данные в файл
                SaveBase64ToFile(base64data, fileName);
                // Обновляем надпись на форме, указывая путь к сохраненному файлу
                label2.Text = $"File saved to {fileName}";
            }
        }
    }
}
