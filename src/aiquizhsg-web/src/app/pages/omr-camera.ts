export type CameraFacing = 'user' | 'environment';

// Ưu tiên đúng mặt camera. Máy không có mặt đó thì mở camera đang có.
export async function openCameraStream(facing: CameraFacing, width: number, height: number): Promise<MediaStream> {
  const attempts: MediaTrackConstraints[] = [
    { facingMode: { exact: facing }, width: { ideal: width }, height: { ideal: height } },
    { facingMode: { ideal: facing }, width: { ideal: width }, height: { ideal: height } },
  ];
  let last: unknown;
  for (const video of attempts) {
    try {
      return await navigator.mediaDevices.getUserMedia({ audio: false, video });
    } catch (error) {
      last = error;
    }
  }
  try {
    return await navigator.mediaDevices.getUserMedia({ audio: false, video: true });
  } catch (error) {
    throw last ?? error;
  }
}

export function cameraDeviceId(stream: MediaStream | null): string {
  return stream?.getVideoTracks()[0]?.getSettings().deviceId ?? '';
}

// Phân biệt máy không có camera với trường hợp người dùng chặn quyền.
export function cameraFailure(error: unknown, fallback: string): string {
  const name = error instanceof DOMException ? error.name : '';
  if (name === 'NotFoundError' || name === 'OverconstrainedError' || name === 'DevicesNotFoundError')
    return 'Không tìm thấy camera. Hãy cắm camera hoặc bật camera trong Windows rồi thử lại.';
  if (name === 'NotReadableError' || name === 'TrackStartError' || name === 'AbortError')
    return 'Camera đang bị ứng dụng khác dùng. Hãy đóng ứng dụng đó rồi thử lại.';
  return fallback;
}
