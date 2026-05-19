# Khoán Chi Phí Hà Lầm

[![Build](https://github.com/Ecotel-com-vn/Khoan_Chi_Phi_Ha_lam/actions/workflows/deploy-release.yml/badge.svg)](https://github.com/Ecotel-com-vn/Khoan_Chi_Phi_Ha_lam/actions/workflows/deploy-release.yml) ![License](https://img.shields.io/badge/license-Internal%20Use%20Only-red) ![Version](https://img.shields.io/badge/version-dev-orange)

Hệ thống quản lý khoán chi phí phục vụ các nghiệp vụ danh mục, đơn giá, sản lượng, quyết toán và báo cáo cho dự án Hà Lầm. Repo hiện tại gồm frontend React/Vite và backend ASP.NET Core 8 Web API dùng PostgreSQL, có build/deploy bằng Docker, Nginx và GitHub Actions.

## Demo / Screenshot

![Nhận diện hệ thống](./frontend/public/ecotel.webp)

Repo hiện chưa có bộ screenshot giao diện chuẩn hóa trong `docs/`; ảnh trên là asset đang được dùng trong frontend.

## Tech Stack

- Frontend: React 19, TypeScript, Vite 7, Tailwind CSS 4, MUI 7, React Router 7, TanStack Table
- Backend: ASP.NET Core 8, C#, MediatR, Entity Framework Core 8, NSwag/Swagger, Serilog
- Database: PostgreSQL
- DevOps: Docker Compose, Nginx reverse proxy, GitHub Actions
- Import/Export: `xlsx` ở frontend, `ClosedXML` ở backend

## Tính Năng Nổi Bật

- Quản lý danh mục: phòng ban, quy trình, vật tư, thiết bị, sản phẩm, đơn vị tính, hệ số và cấu hình
- Quản lý đơn giá và chi phí cho đào lò, xén, lò chợ, điện, sửa chữa, vật tư tiêu hao
- Nhập/xuất Excel cho nhiều phân hệ nghiệp vụ
- Theo dõi kế hoạch, nghiệm thu, quyết toán cuối kỳ và các báo cáo tổng hợp
- Có Swagger để tra cứu API và health check tại `/api/health`

## Getting Started

### 1. Prerequisites

- Node.js `20.x`
- npm `10+`
- .NET SDK `8.0.x`
- Docker Desktop hoặc Docker Engine + Docker Compose
- Git

### 2. Chuẩn bị môi trường

- Frontend: sao chép `frontend/.env.example` thành `frontend/.env.local`
- Backend: tham khảo `backend/Ecotel.KCPCMS.BE/.env.example` để set biến môi trường trong terminal, launch profile hoặc container
- Lưu ý: backend hiện đọc env trực tiếp qua `AddEnvironmentVariables()`, không tự nạp file `.env`

### 3. Khởi động PostgreSQL local

```powershell
cd backend\Ecotel.KCPCMS.BE
docker compose up -d postgres pgadmin
```

Mặc định theo compose này:

- PostgreSQL: `localhost:15432`
- pgAdmin: `http://localhost:8888`

### 4. Chạy backend

```powershell
cd backend\Ecotel.KCPCMS.BE
dotnet restore .\Ecotel.KCPCMS.BE.sln

$env:ASPNETCORE_ENVIRONMENT="Development"
$env:DatabaseSettings__ConnectionString="Host=localhost;Port=15432;Database=<db_name>;Username=<db_user>;Password=<db_password>"
$env:AppSettings__ClientRootAddress="http://localhost:5173/"
$env:CorsSettings__AllowedOrigins="http://localhost:5173;https://localhost:5173;http://localhost:8175;https://localhost:8175"
$env:SwaggerSettings__BaseUrl="https://localhost:8175"

dotnet run --project .\src\Presentation\Host\Host.csproj
```

Truy cập:

- Swagger HTTPS: `https://localhost:8175/swagger`
- Swagger HTTP: `http://localhost:8182/swagger`
- Health check: `https://localhost:8175/api/health`

Nếu máy chưa trust dev certificate:

```powershell
dotnet dev-certs https --trust
```

### 5. Chạy frontend

```powershell
cd frontend
npm install
npm run dev
```

Truy cập ứng dụng tại `http://localhost:5173`.

### 6. Build nhanh để kiểm tra

```powershell
cd frontend
npm run build

cd ..\backend\Ecotel.KCPCMS.BE
dotnet build .\Ecotel.KCPCMS.BE.sln
```

Nếu `dotnet build` báo lỗi file bị khóa bởi tiến trình `Host`, hãy dừng backend đang chạy rồi build lại.

## Environment Variables

Các file mẫu:

- `frontend/.env.example`
- `backend/Ecotel.KCPCMS.BE/.env.example`

Biến tối thiểu để chạy local:

| File                                    | Biến                                 | Bắt buộc | Mô tả                                                            |
| --------------------------------------- | ------------------------------------ | -------- | ---------------------------------------------------------------- |
| `frontend/.env.example`                 | `VITE_API_BASE_URL`                  | Có       | Base URL của backend API, ví dụ `https://localhost:8175/api`     |
| `backend/Ecotel.KCPCMS.BE/.env.example` | `ASPNETCORE_ENVIRONMENT`             | Nên có   | Môi trường chạy backend, thường là `Development`                 |
| `backend/Ecotel.KCPCMS.BE/.env.example` | `ASPNETCORE_URLS`                    | Không    | Override cổng/backend URL nếu không dùng launch profile mặc định |
| `backend/Ecotel.KCPCMS.BE/.env.example` | `DatabaseSettings__ConnectionString` | Có       | Chuỗi kết nối PostgreSQL cho backend                             |
| `backend/Ecotel.KCPCMS.BE/.env.example` | `AppSettings__ServerRootAddress`     | Không    | Base URL public của backend                                      |
| `backend/Ecotel.KCPCMS.BE/.env.example` | `AppSettings__ClientRootAddress`     | Nên có   | Base URL frontend để đồng bộ cấu hình app                        |
| `backend/Ecotel.KCPCMS.BE/.env.example` | `SwaggerSettings__BaseUrl`           | Không    | Base URL hiển thị trong Swagger/OpenAPI                          |
| `backend/Ecotel.KCPCMS.BE/.env.example` | `CorsSettings__AllowedOrigins`       | Nên có   | Danh sách origin frontend được phép gọi API                      |

Ghi chú:

- Giá trị thật không nên commit; chỉ lưu trong `.env.local`, biến môi trường hệ thống, secret của CI/CD hoặc secret manager.
- Repo hiện còn nhiều cấu hình trong `appsettings*.json`; khi tách môi trường staging/release nên override thêm các nhóm bảo mật, mail và storage bằng secret riêng.

## Project Structure

```text
.
├── backend/
│   └── Ecotel.KCPCMS.BE/   # ASP.NET Core API theo hướng Core/Application/Infrastructure/Presentation
├── frontend/               # React + Vite CMS cho người dùng nghiệp vụ
├── deployment/
│   └── release/            # compose và script deploy môi trường release
├── reverse_proxy/          # cấu hình Nginx cho staging/release
├── docker-compose.yaml     # compose tổng hợp ở root, không phải luồng local ưu tiên hiện tại
└── Makefile                # build/push image cho staging và release
```

## Scripts / Commands

| Phạm vi                    | Lệnh                                                       | Mục đích                                     |
| -------------------------- | ---------------------------------------------------------- | -------------------------------------------- |
| `frontend`                 | `npm run dev`                                              | Chạy frontend local                          |
| `frontend`                 | `npm run build`                                            | Build production frontend                    |
| `frontend`                 | `npm run lint`                                             | Kiểm tra ESLint                              |
| `frontend`                 | `npm run format`                                           | Format code bằng Prettier                    |
| `frontend`                 | `npm run preview`                                          | Preview bản build frontend                   |
| `backend/Ecotel.KCPCMS.BE` | `dotnet restore .\Ecotel.KCPCMS.BE.sln`                    | Restore package .NET                         |
| `backend/Ecotel.KCPCMS.BE` | `dotnet build .\Ecotel.KCPCMS.BE.sln`                      | Build toàn bộ backend                        |
| `backend/Ecotel.KCPCMS.BE` | `dotnet run --project .\src\Presentation\Host\Host.csproj` | Chạy API local                               |
| `backend/Ecotel.KCPCMS.BE` | `docker compose up -d postgres pgadmin`                    | Khởi động PostgreSQL và pgAdmin local        |
| repo root                  | `make up`                                                  | Chạy compose tổng hợp ở root theo `Makefile` |
| repo root                  | `make clean`                                               | Dừng stack Docker ở root                     |
| repo root                  | `make staging`                                             | Build và push image staging                  |
| repo root                  | `make release`                                             | Build và push image release                  |

Repo hiện chưa có test project tự động tách riêng để chạy bằng `dotnet test` hoặc `npm test`.

## Contributing

Quy trình đề xuất:

1. Fork hoặc tạo branch từ `main`
2. Đặt tên branch theo convention
3. Commit theo Conventional Commits
4. Mở Pull Request và gắn reviewer phù hợp

Branch naming convention:

- `feature/<module>-<short-description>`
- `fix/<module>-<short-description>`
- `docs/<short-description>`
- `chore/<short-description>`

Commit convention:

- `feat:`
- `fix:`
- `docs:`
- `refactor:`
- `test:`
- `chore:`

PR checklist:

- Đã rebase hoặc cập nhật từ `main`
- Đã tự kiểm tra build/lint ở phần mình thay đổi
- Đã cập nhật README hoặc `.env.example` nếu thêm cấu hình mới
- Đã mô tả rõ phạm vi ảnh hưởng và cách kiểm thử

## License

`Internal Use Only` © 2026 Ecotel.

Repo này phục vụ mục đích nội bộ và hiện chưa phát hành theo giấy phép OSS như MIT/Apache/GPL.
