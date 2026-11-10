IF DB_ID(N'KaraokeDB') IS NULL
	CREATE DATABASE KaraokeDB;
GO

USE KaraokeDB;
GO

IF OBJECT_ID(N'dbo.ChiTietDichVu', N'U') IS NULL
BEGIN
	CREATE TABLE dbo.PhongHat
	(
		MaPhong int IDENTITY(1,1) NOT NULL CONSTRAINT PK_PhongHat PRIMARY KEY,
		TenPhong nvarchar(50) NOT NULL,
		LoaiPhong nvarchar(30) NULL,
		GiaTheoGio decimal(18,2) NOT NULL CONSTRAINT CK_PhongHat_GiaTheoGio CHECK (GiaTheoGio >= 0),
		TrangThai nvarchar(20) NOT NULL CONSTRAINT DF_PhongHat_TrangThai DEFAULT N'Trống'
			CONSTRAINT CK_PhongHat_TrangThai CHECK (TrangThai IN (N'Trống', N'Đang hát'))
	);

	CREATE TABLE dbo.DichVu
	(
		MaDV int IDENTITY(1,1) NOT NULL CONSTRAINT PK_DichVu PRIMARY KEY,
		TenDV nvarchar(100) NOT NULL,
		DonGia decimal(18,2) NOT NULL CONSTRAINT CK_DichVu_DonGia CHECK (DonGia >= 0),
		TonKho int NOT NULL CONSTRAINT CK_DichVu_TonKho CHECK (TonKho >= 0)
	);

	CREATE TABLE dbo.HoaDon
	(
		MaHD int IDENTITY(1,1) NOT NULL CONSTRAINT PK_HoaDon PRIMARY KEY,
		MaPhong int NOT NULL,
		GioVao datetime NOT NULL,
		GioRa datetime NULL,
		TongTienPhong decimal(18,2) NOT NULL CONSTRAINT DF_HoaDon_TongTienPhong DEFAULT 0,
		TongTienDichVu decimal(18,2) NOT NULL CONSTRAINT DF_HoaDon_TongTienDichVu DEFAULT 0,
		TrangThai bit NOT NULL CONSTRAINT DF_HoaDon_TrangThai DEFAULT 0,
		CONSTRAINT FK_HoaDon_PhongHat FOREIGN KEY (MaPhong) REFERENCES dbo.PhongHat(MaPhong)
	);

	CREATE TABLE dbo.ChiTietDichVu
	(
		MaHD int NOT NULL,
		MaDV int NOT NULL,
		SoLuong int NOT NULL CONSTRAINT CK_ChiTietDichVu_SoLuong CHECK (SoLuong > 0),
		DonGia decimal(18,2) NOT NULL CONSTRAINT CK_ChiTietDichVu_DonGia CHECK (DonGia >= 0),
		CONSTRAINT PK_ChiTietDichVu PRIMARY KEY (MaHD, MaDV),
		CONSTRAINT FK_ChiTietDichVu_HoaDon FOREIGN KEY (MaHD) REFERENCES dbo.HoaDon(MaHD),
		CONSTRAINT FK_ChiTietDichVu_DichVu FOREIGN KEY (MaDV) REFERENCES dbo.DichVu(MaDV)
	);

	CREATE INDEX IX_HoaDon_MaPhong_TrangThai ON dbo.HoaDon(MaPhong, TrangThai);
END;
GO

IF NOT EXISTS (SELECT 1 FROM dbo.PhongHat)
BEGIN
	INSERT dbo.PhongHat (TenPhong, LoaiPhong, GiaTheoGio)
	VALUES
		(N'Phòng VIP 01', N'VIP', 250000),
		(N'Phòng VIP 02', N'VIP', 250000),
		(N'Phòng Thường 01', N'Thường', 150000),
		(N'Phòng Thường 02', N'Thường', 150000),
		(N'Phòng Thường 03', N'Thường', 150000),
		(N'Phòng Thường 04', N'Thường', 150000);
END;

IF NOT EXISTS (SELECT 1 FROM dbo.DichVu)
BEGIN
	INSERT dbo.DichVu (TenDV, DonGia, TonKho)
	VALUES
		(N'Bia Heineken', 30000, 100),
		(N'Nước suối', 10000, 100),
		(N'Nước ngọt', 15000, 100),
		(N'Trái cây dĩa', 80000, 30),
		(N'Khăn lạnh', 5000, 100);
END;
GO
