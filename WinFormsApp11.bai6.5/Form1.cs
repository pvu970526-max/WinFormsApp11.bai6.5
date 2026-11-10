using System.Data;
using Microsoft.Data.SqlClient;

namespace WinFormsApp11.bai6._5;

public partial class Form1 : Form
{
    private const string ConnectionString =
        @"Server=(localdb)\MSSQLLocalDB;Database=KaraokeDB;Trusted_Connection=True;TrustServerCertificate=True;";

    private readonly FlowLayoutPanel roomPanel = new();
    private readonly Label selectedRoomLabel = new();
    private readonly Label elapsedLabel = new();
    private readonly ComboBox serviceComboBox = new();
    private readonly NumericUpDown quantityInput = new();
    private readonly DataGridView serviceGrid = new();
    private readonly Button openButton = new();
    private readonly Button payButton = new();
    private readonly Label messageLabel = new();
    private readonly System.Windows.Forms.Timer clock = new();
    private DataTable serviceTable = new();
    private DataRow? selectedRoom;
    private int currentInvoiceId;

    public Form1()
    {
        InitializeComponent();
        BuildInterface();
        clock.Interval = 1000;
        clock.Tick += (_, _) => UpdateElapsedTime();
        clock.Start();
        Shown += (_, _) => LoadRooms();
    }

    private void BuildInterface()
    {
        Text = "Karaoke Management System";
        StartPosition = FormStartPosition.CenterScreen;
        MinimumSize = new Size(1000, 650);
        Size = new Size(1200, 760);

        var title = new Label
        {
            Text = "HỆ THỐNG QUẢN LÝ ĐẶT PHÒNG & DỊCH VỤ KARAOKE",
            Dock = DockStyle.Top,
            Height = 48,
            Font = new Font("Segoe UI", 16, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleCenter,
            BackColor = Color.FromArgb(30, 55, 90),
            ForeColor = Color.White
        };

        roomPanel.Dock = DockStyle.Fill;
        roomPanel.AutoScroll = true;
        roomPanel.Padding = new Padding(12);
        roomPanel.BackColor = Color.FromArgb(245, 247, 250);

        var roomGroup = new GroupBox
        {
            Text = "Sơ đồ phòng hát",
            Dock = DockStyle.Fill,
            Padding = new Padding(8)
        };
        roomGroup.Controls.Add(roomPanel);

        selectedRoomLabel.AutoSize = true;
        selectedRoomLabel.Text = "Chưa chọn phòng";
        selectedRoomLabel.Font = new Font("Segoe UI", 11, FontStyle.Bold);

        elapsedLabel.AutoSize = true;
        elapsedLabel.Text = "Thời gian: -";

        openButton.Text = "Mở phòng";
        openButton.AutoSize = true;
        openButton.Click += OpenRoom;

        payButton.Text = "Thanh toán";
        payButton.AutoSize = true;
        payButton.BackColor = Color.LightGoldenrodYellow;
        payButton.Click += PayInvoice;

        var selectedPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 42,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            Padding = new Padding(4)
        };
        selectedPanel.Controls.AddRange([selectedRoomLabel, elapsedLabel, openButton, payButton]);

        serviceComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
        serviceComboBox.Width = 250;
        quantityInput.Minimum = 1;
        quantityInput.Maximum = 1000;
        quantityInput.Width = 70;

        var addServiceButton = new Button { Text = "Thêm dịch vụ", AutoSize = true };
        addServiceButton.Click += AddService;

        var serviceTop = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 40,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            Padding = new Padding(4)
        };
        serviceTop.Controls.AddRange([
            new Label { Text = "Dịch vụ:", AutoSize = true, Padding = new Padding(0, 7, 0, 0) },
            serviceComboBox,
            new Label { Text = "Số lượng:", AutoSize = true, Padding = new Padding(10, 7, 0, 0) },
            quantityInput,
            addServiceButton
        ]);

        serviceGrid.Dock = DockStyle.Fill;
        serviceGrid.ReadOnly = true;
        serviceGrid.AllowUserToAddRows = false;
        serviceGrid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        serviceGrid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;

        var serviceGroup = new GroupBox
        {
            Text = "Dịch vụ đã gọi (DataTable disconnected)",
            Dock = DockStyle.Fill,
            Padding = new Padding(8)
        };
        serviceGroup.Controls.Add(serviceGrid);
        serviceGroup.Controls.Add(serviceTop);

        messageLabel.Dock = DockStyle.Bottom;
        messageLabel.Height = 30;
        messageLabel.Padding = new Padding(8, 5, 0, 0);
        messageLabel.ForeColor = Color.DarkBlue;

        var bottom = new SplitContainer
        {
            Dock = DockStyle.Fill,
            Orientation = Orientation.Horizontal,
            SplitterDistance = 60,
            Panel1MinSize = 55
        };
        bottom.Panel1.Controls.Add(roomGroup);
        bottom.Panel2.Controls.Add(serviceGroup);
        bottom.Panel2.Controls.Add(selectedPanel);

        Controls.Add(bottom);
        Controls.Add(messageLabel);
        Controls.Add(title);
    }

    private static SqlConnection OpenConnection()
    {
        var connection = new SqlConnection(ConnectionString);
        connection.Open();
        return connection;
    }

    private void LoadRooms()
    {
        try
        {
            using var connection = OpenConnection();
            using var command = new SqlCommand(
                "SELECT MaPhong, TenPhong, LoaiPhong, GiaTheoGio, TrangThai FROM dbo.PhongHat ORDER BY MaPhong",
                connection);
            using var reader = command.ExecuteReader();
            var rooms = new DataTable();
            rooms.Load(reader);

            roomPanel.SuspendLayout();
            roomPanel.Controls.Clear();
            foreach (DataRow room in rooms.Rows)
            {
                var button = new Button
                {
                    Tag = room,
                    Text = $"{room["TenPhong"]}\n{room["TrangThai"]}\n{Convert.ToDecimal(room["GiaTheoGio"]):N0} đ/giờ",
                    Width = 190,
                    Height = 95,
                    Margin = new Padding(8),
                    Font = new Font("Segoe UI", 10, FontStyle.Bold),
                    BackColor = room.Field<string>("TrangThai") == "Trống" ? Color.LightGreen : Color.LightCoral,
                    FlatStyle = FlatStyle.Flat
                };
                button.Click += SelectRoom;
                roomPanel.Controls.Add(button);
            }
            roomPanel.ResumeLayout();
            messageLabel.Text = $"Đã tải {rooms.Rows.Count} phòng.";
        }
        catch (Exception ex)
        {
            ShowError(ex);
        }
    }

    private void SelectRoom(object? sender, EventArgs e)
    {
        selectedRoom = (sender as Button)?.Tag as DataRow;
        if (selectedRoom is null)
            return;

        selectedRoomLabel.Text = $"{selectedRoom["TenPhong"]} ({selectedRoom["TrangThai"]})";
        var isEmpty = selectedRoom.Field<string>("TrangThai") == "Trống";
        openButton.Enabled = isEmpty;
        payButton.Enabled = !isEmpty;
        currentInvoiceId = 0;
        serviceTable = new DataTable();
        serviceGrid.DataSource = serviceTable;

        if (!isEmpty)
        {
            try
            {
                using var connection = OpenConnection();
                currentInvoiceId = GetOpenInvoiceId(connection, selectedRoom.Field<int>("MaPhong"));
                LoadServices(connection, currentInvoiceId);
            }
            catch (Exception ex)
            {
                ShowError(ex);
            }
        }
        else
        {
            serviceComboBox.DataSource = null;
            elapsedLabel.Text = "Thời gian: -";
        }
        UpdateElapsedTime();
    }

    private void OpenRoom(object? sender, EventArgs e)
    {
        if (selectedRoom is null || selectedRoom.Field<string>("TrangThai") != "Trống")
            return;

        try
        {
            using var connection = OpenConnection();
            using var transaction = connection.BeginTransaction();
            using var command = new SqlCommand(
                "INSERT dbo.HoaDon (MaPhong, GioVao, TrangThai) OUTPUT INSERTED.MaHD VALUES (@room, GETDATE(), 0);",
                connection, transaction);
            command.Parameters.AddWithValue("@room", selectedRoom.Field<int>("MaPhong"));
            var invoiceId = (int)command.ExecuteScalar()!;

            command.CommandText = "UPDATE dbo.PhongHat SET TrangThai = N'Đang hát' WHERE MaPhong = @room AND TrangThai = N'Trống';";
            if (command.ExecuteNonQuery() != 1)
                throw new InvalidOperationException("Phòng vừa được mở bởi một người dùng khác.");

            transaction.Commit();
            messageLabel.Text = $"Đã mở phòng, mã hóa đơn: {invoiceId}.";
            LoadRooms();
            SelectRoomButton(selectedRoom.Field<int>("MaPhong"));
        }
        catch (Exception ex)
        {
            ShowError(ex);
        }
    }

    private void AddService(object? sender, EventArgs e)
    {
        if (selectedRoom is null || selectedRoom.Field<string>("TrangThai") == "Trống" || currentInvoiceId == 0)
        {
            messageLabel.Text = "Hãy chọn một phòng đang hát trước khi gọi dịch vụ.";
            return;
        }

        if (serviceComboBox.SelectedValue is not int serviceId || quantityInput.Value <= 0)
            return;

        var quantity = Convert.ToInt32(quantityInput.Value);
        try
        {
            using var connection = OpenConnection();
            using var transaction = connection.BeginTransaction();
            using var stockCommand = new SqlCommand(
                "SELECT TonKho FROM dbo.DichVu WITH (UPDLOCK, HOLDLOCK) WHERE MaDV = @service;",
                connection, transaction);
            stockCommand.Parameters.AddWithValue("@service", serviceId);
            var stockValue = stockCommand.ExecuteScalar();
            if (stockValue is null)
                throw new InvalidOperationException("Dịch vụ không tồn tại.");
            if ((int)stockValue < quantity)
                throw new InvalidOperationException("Số lượng gọi vượt quá tồn kho hiện tại.");

            using var upsert = new SqlCommand(
                "IF EXISTS (SELECT 1 FROM dbo.ChiTietDichVu WHERE MaHD = @invoice AND MaDV = @service) " +
                "UPDATE dbo.ChiTietDichVu SET SoLuong = SoLuong + @quantity WHERE MaHD = @invoice AND MaDV = @service " +
                "ELSE INSERT dbo.ChiTietDichVu (MaHD, MaDV, SoLuong, DonGia) " +
                "SELECT @invoice, MaDV, @quantity, DonGia FROM dbo.DichVu WHERE MaDV = @service;",
                connection, transaction);
            upsert.Parameters.AddWithValue("@invoice", currentInvoiceId);
            upsert.Parameters.AddWithValue("@service", serviceId);
            upsert.Parameters.AddWithValue("@quantity", quantity);
            upsert.ExecuteNonQuery();
            transaction.Commit();

            LoadServices(connection, currentInvoiceId);
            messageLabel.Text = "Đã thêm dịch vụ vào hóa đơn.";
        }
        catch (Exception ex)
        {
            ShowError(ex);
        }
    }

    private void PayInvoice(object? sender, EventArgs e)
    {
        if (selectedRoom is null || currentInvoiceId == 0)
            return;

        try
        {
            using var connection = OpenConnection();
            using var transaction = connection.BeginTransaction(IsolationLevel.Serializable);
            var now = DateTime.Now;
            DateTime startedAt;
            decimal hourlyRate;
            using (var invoiceCommand = new SqlCommand(
                "SELECT h.GioVao, p.GiaTheoGio FROM dbo.HoaDon h JOIN dbo.PhongHat p ON p.MaPhong = h.MaPhong " +
                "WHERE h.MaHD = @invoice AND h.TrangThai = 0;", connection, transaction))
            {
                invoiceCommand.Parameters.AddWithValue("@invoice", currentInvoiceId);
                using var reader = invoiceCommand.ExecuteReader();
                if (!reader.Read())
                    throw new InvalidOperationException("Hóa đơn không tồn tại hoặc đã được thanh toán.");
                startedAt = reader.GetDateTime(0);
                hourlyRate = reader.GetDecimal(1);
            }

            var hours = Math.Max(1, Math.Ceiling((now - startedAt).TotalHours));
            var roomTotal = hourlyRate * (decimal)hours;
            var serviceTotal = serviceTable.Rows.Cast<DataRow>().Sum(row => row.Field<decimal>("ThanhTien"));

            foreach (DataRow service in serviceTable.Rows)
            {
                using var stockCommand = new SqlCommand(
                    "UPDATE dbo.DichVu SET TonKho = TonKho - @quantity WHERE MaDV = @service AND TonKho >= @quantity;",
                    connection, transaction);
                stockCommand.Parameters.AddWithValue("@quantity", service.Field<int>("SoLuong"));
                stockCommand.Parameters.AddWithValue("@service", service.Field<int>("MaDV"));
                if (stockCommand.ExecuteNonQuery() != 1)
                    throw new InvalidOperationException($"Không đủ tồn kho cho dịch vụ: {service["TenDV"]}.");
            }

            using (var updateInvoice = new SqlCommand(
                "UPDATE dbo.HoaDon SET GioRa = @endTime, TongTienPhong = @roomTotal, " +
                "TongTienDichVu = @serviceTotal, TrangThai = 1 WHERE MaHD = @invoice;", connection, transaction))
            {
                updateInvoice.Parameters.AddWithValue("@endTime", now);
                updateInvoice.Parameters.AddWithValue("@roomTotal", roomTotal);
                updateInvoice.Parameters.AddWithValue("@serviceTotal", serviceTotal);
                updateInvoice.Parameters.AddWithValue("@invoice", currentInvoiceId);
                updateInvoice.ExecuteNonQuery();
            }

            using (var updateRoom = new SqlCommand(
                "UPDATE dbo.PhongHat SET TrangThai = N'Trống' WHERE MaPhong = @room;", connection, transaction))
            {
                updateRoom.Parameters.AddWithValue("@room", selectedRoom.Field<int>("MaPhong"));
                updateRoom.ExecuteNonQuery();
            }

            transaction.Commit();
            MessageBox.Show($"Thanh toán thành công.\nTiền phòng: {roomTotal:N0} đ\n" +
                $"Tiền dịch vụ: {serviceTotal:N0} đ\nTổng cộng: {roomTotal + serviceTotal:N0} đ",
                "Thanh toán", MessageBoxButtons.OK, MessageBoxIcon.Information);
            selectedRoom = null;
            currentInvoiceId = 0;
            LoadRooms();
            selectedRoomLabel.Text = "Chưa chọn phòng";
            elapsedLabel.Text = "Thời gian: -";
            serviceGrid.DataSource = null;
            serviceComboBox.DataSource = null;
            openButton.Enabled = false;
            payButton.Enabled = false;
        }
        catch (Exception ex)
        {
            ShowError(ex);
        }
    }

    private void LoadServices(SqlConnection connection, int invoiceId)
    {
        using var adapter = new SqlDataAdapter(
            "SELECT c.MaDV, d.TenDV, c.SoLuong, c.DonGia, " +
            "CAST(c.SoLuong * c.DonGia AS decimal(18,2)) AS ThanhTien " +
            "FROM dbo.ChiTietDichVu c JOIN dbo.DichVu d ON d.MaDV = c.MaDV " +
            "WHERE c.MaHD = @invoice ORDER BY d.TenDV;", connection);
        adapter.SelectCommand!.Parameters.AddWithValue("@invoice", invoiceId);
        serviceTable = new DataTable();
        adapter.Fill(serviceTable);
        serviceGrid.DataSource = serviceTable;
        if (serviceGrid.Columns.Contains("MaDV"))
            serviceGrid.Columns["MaDV"].Visible = false;

        using var serviceAdapter = new SqlDataAdapter(
            "SELECT MaDV, TenDV + N' (còn ' + CONVERT(nvarchar(12), TonKho) + N')' AS TenHienThi, DonGia " +
            "FROM dbo.DichVu WHERE TonKho > 0 ORDER BY TenDV;", connection);
        var services = new DataTable();
        serviceAdapter.Fill(services);
        serviceComboBox.DisplayMember = "TenHienThi";
        serviceComboBox.ValueMember = "MaDV";
        serviceComboBox.DataSource = services;
    }

    private static int GetOpenInvoiceId(SqlConnection connection, int roomId)
    {
        using var command = new SqlCommand(
            "SELECT TOP (1) MaHD FROM dbo.HoaDon WHERE MaPhong = @room AND TrangThai = 0 ORDER BY MaHD DESC;",
            connection);
        command.Parameters.AddWithValue("@room", roomId);
        return command.ExecuteScalar() is int invoiceId ? invoiceId : 0;
    }

    private void SelectRoomButton(int roomId)
    {
        foreach (Button button in roomPanel.Controls.OfType<Button>())
        {
            if ((button.Tag as DataRow)?.Field<int>("MaPhong") == roomId)
            {
                SelectRoom(button, EventArgs.Empty);
                break;
            }
        }
    }

    private void UpdateElapsedTime()
    {
        if (selectedRoom is null || selectedRoom.Field<string>("TrangThai") == "Trống" || currentInvoiceId == 0)
        {
            elapsedLabel.Text = "Thời gian: -";
            return;
        }

        try
        {
            using var connection = OpenConnection();
            using var command = new SqlCommand("SELECT GioVao FROM dbo.HoaDon WHERE MaHD = @invoice;", connection);
            command.Parameters.AddWithValue("@invoice", currentInvoiceId);
            if (command.ExecuteScalar() is DateTime startedAt)
            {
                var elapsed = DateTime.Now - startedAt;
                elapsedLabel.Text = $"Thời gian: {elapsed.Hours:00}:{elapsed.Minutes:00}:{elapsed.Seconds:00}";
            }
        }
        catch
        {
            elapsedLabel.Text = "Thời gian: không kết nối được CSDL";
        }
    }

    private void ShowError(Exception ex)
    {
        messageLabel.Text = ex.Message;
        MessageBox.Show(ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
    }
}
