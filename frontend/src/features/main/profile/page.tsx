import { FormDate } from '@/components/form/form-date';
import { FormInput } from '@/components/form/form-input';
import { FormPassword } from '@/components/form/form-password';
import { FormProvider } from '@/components/form/form-provider';
import { FormSelect } from '@/components/form/form-select';
import { usePopup } from '@/components/popup';
import { Avatar, AvatarFallback, AvatarImage } from '@/components/ui/avatar';
import { Button } from '@/components/ui/button';
import {
	Card,
	CardContent,
	CardDescription,
	CardHeader,
	CardTitle,
} from '@/components/ui/card';
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/components/ui/tabs';
import { useAuthContext } from '@/data/auth/auth-context';
import {
	PASSWORD_FORM_DEFAULT,
	PROFILE_FORM_DEFAULT,
	changePasswordSchema,
	profileFormSchema,
	type ChangePasswordValues,
	type ProfileFormValues,
} from '@/features/main/profile/schema';
import { api } from '@/lib/api';
import { authStorage } from '@/lib/auth-storage';
import { zodResolver } from '@hookform/resolvers/zod';
import {
	Camera,
	FileSignature,
	KeyRound,
	Loader2,
	Save,
	Upload,
	User,
	UserRound,
} from 'lucide-react';
import { useCallback, useEffect, useState } from 'react';
import { useForm } from 'react-hook-form';
import { useSearchParams } from 'react-router-dom';
import { useFileUpload } from '@/hooks/user-file-upload';

const resolveFileUrl = (path?: string | null) => path ?? '';

type EmployeeProfile = {
	id: number;
	fullName: string;
	positionId: number;
	positionName?: string | null;
	departmentId: string;
	departmentName?: string | null;
	userId: number;
	userName?: string | null;
	cccd: string;
	province: string;
	district?: string | null;
	ward?: string | null;
	streetAddress?: string | null;
	dob?: string | null;
	gender?: boolean | null;
	avatar?: string | null;
	email?: string | null;
	phoneNumber?: string | null;
};

type SignatureItem = {
	id: string;
	signatureType: number;
	certificateId?: string | null;
	isPinSaved: boolean;
	isActive: boolean;
};

export default function ProfilePage() {
	const { employeeId, refreshProfile } = useAuthContext();
	const popup = usePopup();
	const [searchParams, setSearchParams] = useSearchParams();

	const activeTab = searchParams.get('tab') || 'profile';
	const [profile, setProfile] = useState<EmployeeProfile | null>(null);
	const [loading, setLoading] = useState(true);

	// Custom hooks quản lý upload & xem trước hình ảnh
	const avatarUpload = useFileUpload();
	const normalSignatureUpload = useFileUpload();
	const initialSignatureUpload = useFileUpload();

	const [savingProfile, setSavingProfile] = useState(false);
	const [savingPassword, setSavingPassword] = useState(false);
	const [savingSignatures, setSavingSignatures] = useState(false);

	const profileForm = useForm<ProfileFormValues>({
		resolver: zodResolver(profileFormSchema),
		defaultValues: PROFILE_FORM_DEFAULT,
		mode: 'onSubmit',
	});

	const passwordForm = useForm<ChangePasswordValues>({
		resolver: zodResolver(changePasswordSchema),
		defaultValues: PASSWORD_FORM_DEFAULT,
		mode: 'onSubmit',
	});

	const loadProfileDetails = useCallback(async () => {
		if (!employeeId) return;
		try {
			setLoading(true);
			const { result } = await api.get<EmployeeProfile>(
				`/v1/User/Employee/${employeeId}/profile`,
			);
			setProfile(result);

			// Gán ảnh S3 hiện tại làm ảnh xem trước ban đầu
			if (result.avatar) {
				avatarUpload.setPreviewUrl(resolveFileUrl(result.avatar));
			}

			// Map values to form
			profileForm.reset({
				fullName: result.fullName || '',
				email: result.email || '',
				phoneNumber: result.phoneNumber || '',
				cccd: result.cccd || '',
				dob: result.dob || null,
				gender:
					result.gender !== null && result.gender !== undefined
						? (String(result.gender) as 'true' | 'false')
						: 'true',
				province: result.province || '',
				district: result.district || '',
				ward: result.ward || '',
				streetAddress: result.streetAddress || '',
				positionId: result.positionId || 0,
				departmentId: result.departmentId || '',
			});
		} catch (error) {
			popup.error('Không thể tải thông tin tài khoản');
			console.error(error);
		} finally {
			setLoading(false);
		}
	}, [employeeId, profileForm, popup]);

	const loadSignatures = useCallback(async () => {
		if (!employeeId) return;
		try {
			const { result } = await api.get<SignatureItem[]>(
				`/v1/User/Employee/${employeeId}/signature`,
			);

			const normal = result?.find((s) => s.signatureType === 2);
			const initial = result?.find((s) => s.signatureType === 1);

			const fetchSignatureStream = async (signatureId: string) => {
				const token = authStorage.getToken();
				const response = await fetch(
					`${import.meta.env.VITE_API_BASE_URL}/v1/User/Employee/signatures/${signatureId}/file`,
					{
						headers: {
							Authorization: `Bearer ${token}`,
						},
					},
				);
				if (!response.ok) throw new Error('Failed to fetch signature stream');
				const blob = await response.blob();
				return URL.createObjectURL(blob);
			};

			if (normal?.id) {
				try {
					normalSignatureUpload.setPreviewUrl(
						await fetchSignatureStream(normal.id),
					);
				} catch (error) {
					console.error('Failed to load normal signature image', error);
				}
			}
			if (initial?.id) {
				try {
					initialSignatureUpload.setPreviewUrl(
						await fetchSignatureStream(initial.id),
					);
				} catch (error) {
					console.error('Failed to load initial signature image', error);
				}
			}
		} catch (error) {
			console.error('Không thể tải thông tin chữ ký:', error);
		}
	}, [employeeId]);

	useEffect(() => {
		if (employeeId) {
			loadProfileDetails();
			loadSignatures();
		}
	}, [employeeId, loadProfileDetails, loadSignatures]);

	const handleTabChange = (val: string) => {
		setSearchParams({ tab: val });
	};

	const handleAvatarSelect = (e: React.ChangeEvent<HTMLInputElement>) => {
		const files = e.target.files;
		if (files && files.length > 0) {
			avatarUpload.selectFile(files[0]); // Lưu tệp cục bộ và hiển thị preview
		}
	};

	const handleSignatureSelect = (
		e: React.ChangeEvent<HTMLInputElement>,
		type: number,
	) => {
		const files = e.target.files;
		if (files && files.length > 0) {
			if (type === 1) {
				normalSignatureUpload.selectFile(files[0]);
			} else {
				initialSignatureUpload.selectFile(files[0]);
			}
		}
	};

	const onProfileSubmit = async (values: ProfileFormValues) => {
		if (!employeeId || !profile) return;
		try {
			setSavingProfile(true);

			// Chỉ upload ảnh đại diện lên S3 khi nhấn nút Lưu thay đổi
			if (avatarUpload.file) {
				const avatarPath = await avatarUpload.upload();

				// Gọi API cập nhật ảnh đại diện trong Database
				await api.patch('/v1/User/Employee/avatar', {
					employeeId,
					avatarUrl: avatarPath,
				});
			}

			// Cập nhật các thông tin cá nhân cơ bản
			await api.put(`/v1/User/Employee/${employeeId}`, {
				fullName: values.fullName,
				positionId: values.positionId,
				departmentId: values.departmentId,
				province: values.province,
				district: values.district,
				ward: values.ward,
				streetAddress: values.streetAddress,
				dob: values.dob,
				gender: values.gender === 'true',
				cccd: values.cccd,
				phoneNumber: values.phoneNumber,
				email: values.email,
			});

			popup.success('Cập nhật thông tin cá nhân thành công');
			avatarUpload.clear(); // Giải phóng RAM và trạng thái file tạm
			await loadProfileDetails();
			await refreshProfile();
		} catch (error) {
			popup.error('Cập nhật thông tin thất bại');
			console.error(error);
		} finally {
			setSavingProfile(false);
		}
	};

	const handleSaveSignatures = async () => {
		if (!employeeId) return;
		try {
			setSavingSignatures(true);

			// Tải chữ ký thường lên S3 khi Lưu cấu hình chữ ký
			if (normalSignatureUpload.file) {
				const path = await normalSignatureUpload.upload({
					queryParams: { signatureType: '2' },
				});
				await api.patch('/v1/User/Employee/signature', {
					employeeId,
					signatureType: 2,
					signatureFileUrl: path,
				});
				normalSignatureUpload.clear();
			}

			// Tải chữ ký nháy lên S3 khi Lưu cấu hình chữ ký
			if (initialSignatureUpload.file) {
				const path = await initialSignatureUpload.upload({
					queryParams: { signatureType: '1' },
				});
				await api.patch('/v1/User/Employee/signature', {
					employeeId,
					signatureType: 1,
					signatureFileUrl: path,
				});
				initialSignatureUpload.clear();
			}

			popup.success('Cập nhật cấu hình chữ ký thành công');
			await loadSignatures();
		} catch (error: any) {
			popup.error(error.message || 'Lỗi lưu trữ chữ ký');
		} finally {
			setSavingSignatures(false);
		}
	};

	const onPasswordSubmit = async (values: ChangePasswordValues) => {
		if (!employeeId) return;
		try {
			setSavingPassword(true);
			await api.patch('/v1/User/Employee/password', {
				employeeId,
				currentPassword: values.currentPassword,
				newPassword: values.newPassword,
			});
			popup.success('Đổi mật khẩu thành công');
			passwordForm.reset(PASSWORD_FORM_DEFAULT);
		} catch (error: any) {
			popup.error(error.message || 'Mật khẩu cũ không chính xác');
		} finally {
			setSavingPassword(false);
		}
	};

	if (loading && !profile) {
		return (
			<div className='flex h-96 w-full items-center justify-center'>
				<Loader2 className='text-primary size-8 animate-spin' />
				<span className='ms-2 text-sm text-slate-500'>
					Đang tải thông tin tài khoản...
				</span>
			</div>
		);
	}

	return (
		<div className='mx-auto max-w-7xl px-4 py-8 sm:px-6 lg:px-8'>
			<div className='mb-6 flex flex-col justify-between gap-4 border-b pb-5 sm:flex-row sm:items-center'>
				<div>
					<h3 className='text-2xl leading-6 font-bold text-slate-900'>
						Hồ sơ cá nhân
					</h3>
					<p className='mt-2 max-w-4xl text-sm text-slate-500'>
						Quản lý thông tin cá nhân, ảnh đại diện, chữ ký số và mật khẩu của
						bạn.
					</p>
				</div>
			</div>

			<div className='grid grid-cols-1 gap-6 lg:grid-cols-3'>
				{/* Left Sidebar */}
				<div className='space-y-6 lg:col-span-1'>
					<Card className='shadow-lg'>
						<CardContent className='pt-6 text-center'>
							<div className='relative mx-auto mb-4 size-32'>
								<Avatar className='h-full w-full border-4 border-slate-100 shadow-md'>
									<AvatarImage
										src={avatarUpload.previewUrl}
										className='object-cover'
									/>
									<AvatarFallback className='bg-slate-50 text-slate-400'>
										<UserRound className='size-16' strokeWidth={1} />
									</AvatarFallback>
								</Avatar>
								<label
									htmlFor='avatar-upload'
									className='absolute right-0 bottom-0 flex size-10 cursor-pointer items-center justify-center rounded-full border border-slate-200 bg-white shadow-md hover:bg-slate-50'
								>
									{avatarUpload.uploading ? (
										<Loader2 className='size-5 animate-spin text-slate-500' />
									) : (
										<Camera className='size-5 text-slate-600' />
									)}
									<input
										id='avatar-upload'
										type='file'
										accept='image/*'
										className='hidden'
										onChange={handleAvatarSelect}
										disabled={avatarUpload.uploading}
									/>
								</label>
							</div>

							<h4 className='text-lg font-bold text-slate-900'>
								{profile?.fullName}
							</h4>
							<p className='text-sm text-slate-500'>@{profile?.userName}</p>

							<div className='mt-6 space-y-3 border-t pt-4 text-start text-sm text-slate-600'>
								<div className='flex justify-between'>
									<span className='font-medium text-slate-400'>Chức vụ:</span>
									<span className='font-semibold text-slate-900'>
										{profile?.positionName || 'Chưa cập nhật'}
									</span>
								</div>
								<div className='flex justify-between'>
									<span className='font-medium text-slate-400'>
										Phòng ban/Đơn vị:
									</span>
									<span className='font-semibold text-slate-900'>
										{profile?.departmentName || 'Chưa cập nhật'}
									</span>
								</div>
							</div>
						</CardContent>
					</Card>

					{/* Signature Quick Preview Card */}
					<Card className='shadow-lg'>
						<CardHeader className='pb-2'>
							<CardTitle className='flex items-center gap-2 text-base font-bold'>
								<FileSignature className='size-4 text-slate-500' />
								Xem trước chữ ký
							</CardTitle>
						</CardHeader>
						<CardContent className='space-y-4 pt-2 text-center text-sm'>
							<div className='grid grid-cols-2 gap-4'>
								<div className='rounded-md border bg-slate-50 p-2'>
									<p className='mb-2 text-xs font-medium text-slate-400'>
										Chữ ký thường
									</p>
									<div className='flex h-20 items-center justify-center overflow-hidden rounded border border-dashed bg-white'>
										{normalSignatureUpload.previewUrl ? (
											<img
												src={normalSignatureUpload.previewUrl}
												alt='Chữ ký thường'
												className='max-h-full max-w-full object-contain'
											/>
										) : (
											<span className='text-xs text-slate-400 italic'>
												Chưa cài đặt
											</span>
										)}
									</div>
								</div>
								<div className='rounded-md border bg-slate-50 p-2'>
									<p className='mb-2 text-xs font-medium text-slate-400'>
										Chữ ký nháy
									</p>
									<div className='flex h-20 items-center justify-center overflow-hidden rounded border border-dashed bg-white'>
										{initialSignatureUpload.previewUrl ? (
											<img
												src={initialSignatureUpload.previewUrl}
												alt='Chữ ký nháy'
												className='max-h-full max-w-full object-contain'
											/>
										) : (
											<span className='text-xs text-slate-400 italic'>
												Chưa cài đặt
											</span>
										)}
									</div>
								</div>
							</div>
						</CardContent>
					</Card>
				</div>

				{/* Right Content Tabs */}
				<div className='lg:col-span-2'>
					<Card className='shadow-lg'>
						<CardContent className='p-6'>
							<Tabs
								value={activeTab}
								onValueChange={handleTabChange}
								className='w-full'
							>
								<TabsList className='mb-6 grid w-full grid-cols-3 rounded-lg bg-slate-100/50 p-1'>
									<TabsTrigger
										value='profile'
										className='rounded-md py-2 text-sm font-medium'
									>
										<User className='mr-2 size-4' />
										Thông tin cá nhân
									</TabsTrigger>
									<TabsTrigger
										value='signatures'
										className='rounded-md py-2 text-sm font-medium'
									>
										<FileSignature className='mr-2 size-4' />
										Chữ ký & Đóng dấu
									</TabsTrigger>
									<TabsTrigger
										value='password'
										className='rounded-md py-2 text-sm font-medium'
									>
										<KeyRound className='mr-2 size-4' />
										Đổi mật khẩu
									</TabsTrigger>
								</TabsList>

								{/* Personal Info Tab */}
								<TabsContent
									value='profile'
									className='space-y-4 focus-visible:outline-none'
								>
									<CardDescription className='mb-4'>
										Cập nhật thông tin chi tiết về lý lịch cá nhân của bạn.
									</CardDescription>

									<FormProvider
										context={profileForm}
										onSubmit={onProfileSubmit}
									>
										<div className='grid grid-cols-1 gap-4 md:grid-cols-2'>
											<FormInput
												control={profileForm.control}
												name='fullName'
												label='Họ và tên'
												placeholder='Nhập họ và tên'
											/>
											<FormInput
												control={profileForm.control}
												name='cccd'
												label='Số CCCD / Hộ chiếu'
												placeholder='Nhập số CCCD'
											/>
										</div>

										<div className='grid grid-cols-1 gap-4 md:grid-cols-2'>
											<FormInput
												control={profileForm.control}
												name='email'
												label='Thư điện tử (Email)'
												placeholder='Nhập địa chỉ email'
											/>
											<FormInput
												control={profileForm.control}
												name='phoneNumber'
												label='Số điện thoại'
												placeholder='Nhập số điện thoại'
											/>
										</div>

										<div className='grid grid-cols-1 gap-4 md:grid-cols-2'>
											<FormDate
												control={profileForm.control}
												name='dob'
												label='Ngày sinh'
											/>
											<FormSelect
												control={profileForm.control}
												name='gender'
												label='Giới tính'
												placeholder='Chọn giới tính'
												options={[
													{ value: 'true', label: 'Nam' },
													{ value: 'false', label: 'Nữ' },
												]}
											/>
										</div>

										<div className='my-4 border-t pt-4'>
											<h5 className='mb-4 text-sm font-bold text-slate-800'>
												Địa chỉ thường trú
											</h5>
											<div className='grid grid-cols-1 gap-4 md:grid-cols-2'>
												<FormInput
													control={profileForm.control}
													name='province'
													label='Tỉnh / Thành phố'
													placeholder='Ví dụ: Quảng Ninh'
												/>
												<FormInput
													control={profileForm.control}
													name='district'
													label='Quận / Huyện'
													placeholder='Ví dụ: Cẩm Phả'
												/>
											</div>
											<div className='mt-4 grid grid-cols-1 gap-4 md:grid-cols-2'>
												<FormInput
													control={profileForm.control}
													name='ward'
													label='Phường / Xã'
													placeholder='Ví dụ: Cẩm Phả'
												/>
												<FormInput
													control={profileForm.control}
													name='streetAddress'
													label='Số nhà / Tên đường'
													placeholder='Ví dụ: Số 123 đường Trần Phú'
												/>
											</div>
										</div>

										<div className='flex justify-end border-t pt-4'>
											<Button
												type='submit'
												className='gap-2'
												disabled={savingProfile}
											>
												{savingProfile ? (
													<Loader2 className='size-4 animate-spin' />
												) : (
													<Save className='size-4' />
												)}
												Lưu thay đổi
											</Button>
										</div>
									</FormProvider>
								</TabsContent>

								{/* Signatures Tab */}
								<TabsContent
									value='signatures'
									className='space-y-6 focus-visible:outline-none'
								>
									<div className='border-b pb-4'>
										<CardTitle className='text-lg font-semibold'>
											Quản lý ảnh chữ ký
										</CardTitle>
										<CardDescription className='mt-1'>
											Tải lên chữ ký thường và chữ ký nháy để sử dụng cho việc
											duyệt và đóng dấu văn bản điện tử.
										</CardDescription>
									</div>

									<div className='grid grid-cols-1 gap-6 md:grid-cols-2'>
										{/* Standard Signature Upload */}
										<Card className='border border-slate-100 bg-slate-50/50 shadow-none'>
											<CardHeader className='pb-3'>
												<CardTitle className='text-sm font-bold'>
													Chữ ký thường (Normal Signature)
												</CardTitle>
												<CardDescription className='text-xs'>
													Hiển thị trên phần đóng dấu ký tên chính thức.
												</CardDescription>
											</CardHeader>
											<CardContent className='space-y-4'>
												<div className='group relative flex h-40 w-full items-center justify-center overflow-hidden rounded border border-dashed bg-white p-2'>
													{normalSignatureUpload.previewUrl ? (
														<>
															<img
																src={normalSignatureUpload.previewUrl}
																alt='Chữ ký thường'
																className='max-h-full max-w-full object-contain'
															/>
															<div className='absolute inset-0 flex items-center justify-center rounded bg-black/40 opacity-0 transition-opacity group-hover:opacity-100'>
																<label
																	htmlFor='sig-normal-upload'
																	className='flex cursor-pointer items-center gap-1.5 rounded-md bg-white px-3 py-1.5 text-xs font-semibold text-slate-800 shadow-sm hover:bg-slate-50'
																>
																	<Upload className='size-3.5' /> Cập nhật ảnh
																	mới
																</label>
															</div>
														</>
													) : (
														<label
															htmlFor='sig-normal-upload'
															className='flex cursor-pointer flex-col items-center justify-center p-4 text-center'
														>
															<Upload className='mb-2 size-8 text-slate-400' />
															<span className='text-xs font-semibold text-slate-600'>
																Bấm vào để tải lên
															</span>
															<span className='mt-1 text-[10px] text-slate-400'>
																Định dạng JPG, PNG, WEBP
															</span>
														</label>
													)}
													<input
														id='sig-normal-upload'
														type='file'
														accept='image/*'
														className='hidden'
														onChange={(e) => handleSignatureSelect(e, 1)}
														disabled={normalSignatureUpload.uploading}
													/>
												</div>
												{normalSignatureUpload.uploading && (
													<div className='flex items-center justify-center gap-1.5 text-xs text-slate-500'>
														<Loader2 className='size-3 animate-spin' /> Đang lưu
														chữ ký thường...
													</div>
												)}
											</CardContent>
										</Card>

										{/* Initial Signature Upload */}
										<Card className='border border-slate-100 bg-slate-50/50 shadow-none'>
											<CardHeader className='pb-3'>
												<CardTitle className='text-sm font-bold'>
													Chữ ký nháy (Initial Signature)
												</CardTitle>
												<CardDescription className='text-xs'>
													Hiển thị trên phần phê duyệt lề của các trang văn bản.
												</CardDescription>
											</CardHeader>
											<CardContent className='space-y-4'>
												<div className='group relative flex h-40 w-full items-center justify-center overflow-hidden rounded border border-dashed bg-white p-2'>
													{initialSignatureUpload.previewUrl ? (
														<>
															<img
																src={initialSignatureUpload.previewUrl}
																alt='Chữ ký nháy'
																className='max-h-full max-w-full object-contain'
															/>
															<div className='absolute inset-0 flex items-center justify-center rounded bg-black/40 opacity-0 transition-opacity group-hover:opacity-100'>
																<label
																	htmlFor='sig-initial-upload'
																	className='flex cursor-pointer items-center gap-1.5 rounded-md bg-white px-3 py-1.5 text-xs font-semibold text-slate-800 shadow-sm hover:bg-slate-50'
																>
																	<Upload className='size-3.5' /> Cập nhật ảnh
																	mới
																</label>
															</div>
														</>
													) : (
														<label
															htmlFor='sig-initial-upload'
															className='flex cursor-pointer flex-col items-center justify-center p-4 text-center'
														>
															<Upload className='mb-2 size-8 text-slate-400' />
															<span className='text-xs font-semibold text-slate-600'>
																Bấm vào để tải lên
															</span>
															<span className='mt-1 text-[10px] text-slate-400'>
																Định dạng JPG, PNG, WEBP
															</span>
														</label>
													)}
													<input
														id='sig-initial-upload'
														type='file'
														accept='image/*'
														className='hidden'
														onChange={(e) => handleSignatureSelect(e, 0)}
														disabled={initialSignatureUpload.uploading}
													/>
												</div>
												{initialSignatureUpload.uploading && (
													<div className='flex items-center justify-center gap-1.5 text-xs text-slate-500'>
														<Loader2 className='size-3 animate-spin' /> Đang lưu
														chữ ký nháy...
													</div>
												)}
											</CardContent>
										</Card>
									</div>

									<div className='flex w-full justify-end border-t pt-4'>
										<Button
											type='button'
											onClick={handleSaveSignatures}
											className='gap-2'
											disabled={
												savingSignatures ||
												normalSignatureUpload.uploading ||
												initialSignatureUpload.uploading
											}
										>
											{savingSignatures ? (
												<Loader2 className='size-4 animate-spin' />
											) : (
												<Save className='size-4' />
											)}
											Lưu cấu hình chữ ký
										</Button>
									</div>
								</TabsContent>

								{/* Change Password Tab */}
								<TabsContent
									value='password'
									className='space-y-4 focus-visible:outline-none'
								>
									<CardDescription className='mb-4'>
										Đảm bảo an toàn cho tài khoản bằng cách thay đổi mật khẩu
										định kỳ.
									</CardDescription>

									<FormProvider
										context={passwordForm}
										onSubmit={onPasswordSubmit}
									>
										<div className='max-w-md space-y-4'>
											<FormPassword
												control={passwordForm.control}
												name='currentPassword'
												label='Mật khẩu hiện tại'
												placeholder='Nhập mật khẩu hiện tại'
											/>
											<FormPassword
												control={passwordForm.control}
												name='newPassword'
												label='Mật khẩu mới'
												placeholder='Tối thiểu 6 ký tự'
											/>
											<FormPassword
												control={passwordForm.control}
												name='confirmNewPassword'
												label='Xác nhận mật khẩu mới'
												placeholder='Nhập lại mật khẩu mới'
											/>
										</div>

										<div className='mt-6 flex justify-start border-t pt-4'>
											<Button
												type='submit'
												className='gap-2'
												disabled={savingPassword}
											>
												{savingPassword ? (
													<Loader2 className='size-4 animate-spin' />
												) : (
													<KeyRound className='size-4' />
												)}
												Đổi mật khẩu
											</Button>
										</div>
									</FormProvider>
								</TabsContent>
							</Tabs>
						</CardContent>
					</Card>
				</div>
			</div>
		</div>
	);
}
