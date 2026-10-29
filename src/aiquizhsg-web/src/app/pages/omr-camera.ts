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
