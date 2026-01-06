using Gif.Components;
using System.Diagnostics;
using System.Drawing.Imaging;
using System.Numerics;
using System.Text.RegularExpressions;

namespace Illusions
{
	public partial class MyForm : Form {
		private readonly int Frames = 10;
		private readonly int W = 1920, H = 1080;
		private readonly int SelectedFps = 60;
		readonly int CellVerticalCount = 9;
		private readonly int Repeat = 1;
		private readonly float SwirlAlpha = 0.5f;

		private readonly int cellSize, cellWidth;
		private readonly Bitmap[] bmp;
		private readonly MemoryStream?[] msPngs;
		private readonly AnimatedGifEncoder encoder;
		private readonly Vector3[] colors;
		int frame;
		// unused for parallel gif encoding
		//private readonly BitmapData[] locked;

		public MyForm() {
			InitializeComponent();
			frame = 0;
			screen.Width = W;
			screen.Height = H;
			Width = W + 32;
			Height = H + 64;
			cellSize = H / CellVerticalCount;
			bmp = new Bitmap[Frames];
			// unused for parallel gif encoding
			//locked = new BitmapData[Frames];
			//tasks = new Task[Frames];
			//token = cts.Token;
			encoder = new();
			encoder.SetDelay(5);
			encoder.SetRepeat(0);        // Loop
			encoder.SetQuality(1);       // Highest quality
			encoder.SetTransparent(Color.Empty);
			encoder.Start(W, H, "output.gif");
			msPngs = new MemoryStream[Frames];
			colors = new Vector3[(cellWidth = W / cellSize + 2) * (H / cellSize + 2)];
			Random r = new();
			for (int i = colors.Length; 0 <= --i; colors[i] = Hsv(r.Next(360), 1, 1)) { }
			for (int i = 0; i < Frames; ++i) DrawSwirl(i);
			encoder.Finish();
			// unused for parallel gif encoding
			/*while (!token.IsCancellationRequested && encoder.TryWrite() != TryWrite.Failed)
				if (encoder.IsFinished()) break;
			for (int i = 0; i < Frames; ++i) bmp[i].UnlockBits(locked[i]);*/
			ExportEnd();
		}
		// unused for parallel gif encoding
		//private readonly Task[] tasks;
		//int taskIndex = 0;
		//CancellationTokenSource cts = new();
		//CancellationToken token;
		private void ExportFrameMp4(Bitmap b, int index) {
			msPngs[index] = new MemoryStream();
			b.Save(msPngs[index], ImageFormat.Png);
			msPngs[index].Flush();
		}
		private unsafe void ExportFrameGif(byte* pixels, int stride) {
			encoder.AddFrame(pixels, stride);
			// unused for parallel gif encoding
			//encoder.AddFrameParallel(pixels, stride, ref tasks[taskIndex++],token);
		}
		private void ExportEnd() {
			SavePngsToMp4("output.mp4");
			if (msPngs != null)
			for (int i = 0; i < msPngs.Length; ++i) {
				msPngs[i]?.Dispose();
				msPngs[i] = null;
			}
		}
		private Vector3 Sample(float x, float y) {
			/*int xc = (int)x, yc = (int)y;
			y -= yc;
			x -= xc;
			int i = xc + yc * cw;
			var left = (1 - y) * colors[i] + y * colors[i + cw];
			var right = (1 - y) * colors[i + 1] + y * colors[i + cw + 1];
			return left * (1 - x) + x * right;*/ // unused bilinear sampling
			return colors[(int)x + (int)y * cellWidth];
		}
		private void DrawSwirl(int index) {
			var b = bmp[index] = new Bitmap(screen.Width, screen.Height);
			var locked = b.LockBits(
				new Rectangle(0, 0, screen.Width, screen.Height),
				ImageLockMode.WriteOnly,
				PixelFormat.Format24bppRgb);
			unsafe {
				byte* bmpPtr = (byte*)(void*)locked.Scan0;
				var invpi = 16 / MathF.PI;
				int stride = locked.Stride;
				//var di = index * repeat * 3.0f / Frames;
				_ = Parallel.For(0, screen.Height, y => {
					var yc = (y % cellSize) - cellSize / 2;
					var yy = yc * yc;
					byte* row = bmpPtr + y * stride;
					for (int x = 0; x < screen.Width; ++x) {
						var xc = (x % cellSize) - cellSize / 2;
						var xx = xc * xc;
						var d = 1.0f / (1 + (xx + yy) / 24);
						var tan = (MathF.Atan2((y / cellSize % 2) + (x / cellSize % 2) == 1 ? yc : -yc, xc)) * invpi + 2.0f * Repeat * index / Frames + 1024 * Math.PI;
						var a = Math.Max(d, 1 - SwirlAlpha + SwirlAlpha * MathF.Abs((float)tan % 2 - 1)) * Sample((float)x / cellSize, (float)y / cellSize);
						row[0] = (byte)a.X;
						row[1] = (byte)a.Y;
						row[2] = (byte)a.Z;
						row += 3;
					}
				});
				ExportFrameGif(bmpPtr, stride);
			}
			b.UnlockBits(locked);
			ExportFrameMp4(b, index);
		}

		/* unfinished different illusion
		readonly int strength = 9;
		private void DrawCircle(int index) {
			var b = bmp[index] = new Bitmap(screen.Width, screen.Height);
			var locked = b.LockBits(
				new Rectangle(0, 0, screen.Width, screen.Height),
				ImageLockMode.WriteOnly,
				PixelFormat.Format24bppRgb);
			unsafe {
				byte* bmpPtr = (byte*)(void*)locked.Scan0;
				var invpi = 1.5f / MathF.PI;
				var di = index * 3.0f / Frames;

				float s = 80.0f / strength, e = 120.0f / strength;
				_ = Parallel.For(0, screen.Height, y => {
					var yc = y - 256;
					var yy = yc * yc;
					byte* row = bmpPtr + y * screen.Width * 3;
					for (int x = 0; x < screen.Width; ++x) {
						var xc = x - 256;
						var xx = xc * xc;
						var d = MathF.Sqrt(xx + yy) / strength;
						var tan = MathF.Atan2(yc, xc) * invpi;

						var r = 1.0f - MathF.Min(1.0f, MathF.Min(MathF.Abs((tan - di) % 3), 3 - MathF.Abs((tan - di) % 3)));
						var ra = (tan - di + 7) % 3;
						ra /= 2;
						ra += d;
						ra = MathF.Max(0, MathF.Min(e - ra, MathF.Min(ra - s, 1)));

						var g = 1.0f - MathF.Min(1.0f, MathF.Min(MathF.Abs((tan - di + 1) % 3), 3 - MathF.Abs((tan - di + 1) % 3)));
						var ga = (tan - di + 8) % 3;
						ga /= 2;
						ga += d;
						ga = MathF.Max(0, MathF.Min(e - ga, MathF.Min(ga - s, 1)));


						var b = 1.0f - MathF.Min(1.0f, MathF.Min(MathF.Abs((tan - di + 2) % 3), 3 - MathF.Abs((tan - di + 2) % 3)));
						var ba = (tan - di + 9) % 3;
						ba /= 2;
						ba += d;
						ba = MathF.Max(0, MathF.Min(e - ba, MathF.Min(ba - s, 1)));

						row[0] = (byte)(ba * b * 255);
						row[1] = (byte)(ga * g * 255);
						row[2] = (byte)(ra * r * 255);
						row += 3;
					}
				});
				ExportFrameGif(bmpPtr, stride);
			}
			b.UnlockBits(locked);
			ExportFrameMp4(b, index);
		}
		*/

		// animates teh preview window
		private void timer_Tick(object sender, EventArgs e) {
			frame = (frame + 1) % Frames;
			screen.Image = bmp[frame];
		}
		// converts hsv to rgb
		private static Vector3 Hsv(double h, double s, double v) {
			double r, g, b;
			if (s <= 0) {
				r = v; g = v; b = v;
			} else {
				int i;
				double f, p, q, t;
				h = h == 360 ? 0 : h / 60;
				i = (int)Math.Truncate(h);
				f = h - i;
				p = v * (1.0 - s);
				q = v * (1.0 - (s * f));
				t = v * (1.0 - (s * (1.0 - f)));
				switch (i) {
					case 0: r = v; g = t; b = p; break;
					case 1: r = q; g = v; b = p; break;
					case 2: r = p; g = v; b = t; break;
					case 3: r = p; g = q; b = v; break;
					case 4: r = t; g = p; b = v; break;
					default: r = v; g = p; b = q; break;
				}
			}
			return new Vector3((byte)(255 * r), (byte)(255 * g), (byte)(255 * b));
		}
		//exports mp4 using ffmpeg.exe
		private int pngFailed, encodedMp4;
		private readonly int MaxPngFails = 50;
		private string SavePngsToMp4(string mp4Path) {
			encodedMp4 = 0;
			var ffmpegPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ffmpeg.exe");
			if (!File.Exists(ffmpegPath))
				return Fail("Ffmpeg.exe not found"); // if ffmpeg doesn't exist, return failure immediately
			try {
				File.Delete(mp4Path);  // Delete existing file if present
			} catch (IOException ex) {
				return Fail("Failed to delete existing file: " + ex.Message); // return failure if deletion fails
			}
			// Start FFmpeg in a parallel process to encode the PNG sequence
			using var ffmpegProcess = new Process {
				StartInfo = new ProcessStartInfo {
					FileName = ffmpegPath,
					Arguments = $"-y -framerate {SelectedFps} -f image2pipe -vcodec png -i pipe:0 -vf \"scale=iw:ih\" -movflags +faststart -c:v libx264 -profile:v high444 -level 5.2 -preset veryslow -crf 18 -pix_fmt yuv444p \"{mp4Path}\"",
					UseShellExecute = false,
					RedirectStandardInput = true,
					RedirectStandardError = true, // will get progress from this
					CreateNoWindow = true
				}
			};
			string fail = ""; // setup error listener
			try {
				// start ffmpeg
				if (!ffmpegProcess.Start())
					return Fail("Ffmpeg failed to start");
				pngFailed = 0; // reset failure attempt counter, every png write fail will increment it, and if it reaches 1000 it will cancel the FinishTasks
				// report completion
				var frameRegex = new Regex(@"frame=\s*(\d+)", RegexOptions.Compiled);
				ffmpegProcess.ErrorDataReceived += (s, e) => {
					if (e.Data == null) return;
					var match = frameRegex.Match(e.Data);
					if (match.Success) {
						encodedMp4 = ushort.Parse(match.Groups[1].Value);
					}
				};
				ffmpegProcess.BeginErrorReadLine();
				// will check if that other parallel thread elsewhere finished exporting all the pngs into the memory streams, and will dump these streams sequentially into the ffmpeg's input
				using (var inputStream = ffmpegProcess.StandardInput.BaseStream) {
					for (int enc = 0; /*!token.IsCancellationRequested &&*/ enc < Frames; Thread.Sleep(100))
						while (enc < Frames) {
							var ms = msPngs[enc++];
							if (ms == null) return Fail("Memory stream " + (enc - 1) + "not initialized.");
							ms.Position = 0;
							ms.CopyTo(inputStream);  // Write memory stream directly to FFmpeg's input stream
						}
				}
				if (pngFailed >= MaxPngFails /*|| token.IsCancellationRequested*/) { // If the export was cancelled from outside - terminate the ffmpeg process
					if (ffmpegProcess.StandardInput.BaseStream.CanWrite) {
						ffmpegProcess.StandardInput.Write("q");  // Send 'q' to FFmpeg to terminate gracefully
						ffmpegProcess.StandardInput.Flush();     // Ensure the command is sent
						Thread.Sleep(500);  // Give FFmpeg some time to exit gracefully
					}
					if (!ffmpegProcess.HasExited)
						ffmpegProcess.Kill();  // Force terminate if graceful shutdown isn't possible
				}
				// Wait for the process to exit
				ffmpegProcess.WaitForExit();
			} catch (Exception ex) {
				return Fail("Exception: " + ex.Message); // return exception error
			}
			if (pngFailed >= MaxPngFails)
				fail += ";Failed to save PNGs";
			return fail != "" ? Fail("Ffmpeg errors: " + fail) : ""; // return fail or success
		}
		private static string Fail(string log) {
			Console.WriteLine(log);
			return log;
		}
	}
}
