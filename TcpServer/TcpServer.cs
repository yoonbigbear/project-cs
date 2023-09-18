using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Collections.Concurrent;
using System.Reflection.Metadata.Ecma335;
using System.Threading.Tasks.Dataflow;

namespace Net
{
	public class TcpServer : IDisposable
	{
		Socket _acceptorSocket;
		SocketAsyncEventArgs _acceptorEventArg;
		EndPoint _endPoint;

		public bool IsSocketDisposed { get; private set; } = true;
		public bool IsAccepting { get; private set; }
		public bool IsStarted { get; private set; }
		public bool IsDisposed { get; private set; }

		public TcpServer(EndPoint endPoint) => _endPoint = endPoint;
		Socket CreateSocket() => new Socket(_endPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

		public bool Start()
		{
			_acceptorSocket = CreateSocket();
			IsSocketDisposed = false;

			_acceptorEventArg = new SocketAsyncEventArgs();
			_acceptorEventArg.Completed += OnAsyncCompleted;

			//소켓 옵션 설정
			_acceptorSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, false);
			_acceptorSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.NoDelay, true);
			//ip v4 v6 모두 사용 가능하도록 설정
			if (_acceptorSocket.AddressFamily == AddressFamily.InterNetworkV6)
				_acceptorSocket.DualMode = true;

			// Endpoint에 소켓 연결
			_acceptorSocket.Bind(_endPoint);

			//실제 생성된 endpoint로 업데이트
			_endPoint = _acceptorSocket.LocalEndPoint;

			OnStart();

			//최대 팬딩제한 두고 Listen시작.
			_acceptorSocket.Listen();

			IsStarted = true;

			OnStarted();

			//Accept.
			IsAccepting = true;
			StartAccept(_acceptorEventArg);



			return true;
		}
		public bool Stop()
		{

			Debug.Assert(IsStarted);
			if (!IsStarted)
				return false;

			//더이상 새 유저를 받지 않는다. accept 이벤트 콜백도 제거
			IsAccepting = false;
			_acceptorEventArg.Completed -= OnAsyncCompleted;

			OnStop();

			try
			{
				_acceptorSocket.Close();
				_acceptorSocket.Dispose();
				_acceptorEventArg.Dispose();

				IsSocketDisposed = true;
			}
			catch (ObjectDisposedException ex) { throw ex; }

			//연결 되어있는 세션 모두 Disconnect 호출

			IsStarted = false;

			OnStopped();

			return true;
		}


		void StartAccept(SocketAsyncEventArgs args)
		{
			//context가 재사용되고 있어서 소켓을 우선 비워야 한다.
			args.AcceptSocket = null;

			if (!_acceptorSocket.AcceptAsync(args))
				ProcessAccept(args);
		}
		void ProcessAccept(SocketAsyncEventArgs args)
		{
			if (args.SocketError == SocketError.Success)
			{
				OnConnect(args);
			}
			else
			{
				//에러 이슈잉
				Debug.Assert(false, "Socket Accept Failure");
			}

			OnConnected();

			//다시 Accept 재시작
			if (IsAccepting)
				StartAccept(args);
		}

		// Socket.AcceptAsync() 에서 호출되는 완료 콜백.
		void OnAsyncCompleted(object sender, SocketAsyncEventArgs args)
		{
			if (IsSocketDisposed)
				return;

			//다음 Accept 반복
			ProcessAccept(args);
		}

		public void Dispose()
		{
			if (!IsDisposed)
				Stop();

			IsDisposed = true;

			GC.SuppressFinalize(this);
		}
		protected virtual void Dispose(bool disposing)
		{
			if (!IsDisposed)
			{
				if (disposing)
				{
					Stop();
				}
			}

			// Dispose Unmanaged Resource

			// large fields to null

			IsDisposed = true;
		}

		protected virtual void OnStart() {}
		protected virtual void OnStarted() {}
		protected virtual void OnStop() { }
		protected virtual void OnStopped() { }
		protected virtual void OnConnect(SocketAsyncEventArgs arg) { }
		protected virtual void OnConnected() { }
		protected virtual void OnDisconnect() { }
		protected virtual void OnDisconnected() { }
	}
}
