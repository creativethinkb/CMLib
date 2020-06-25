# CMLib (Communication Middleware Library)
<hr/>
CMLib은 .Net Framework 4.5에서 C#으로 개발되었으며, TCPListener, TCPClient, UDPClient, SerialPort, Thread, Queue등의 객체들로 구성된 통신 미들웨어이다.  

## 사용 목적
<hr/>
범용성과 재사용성을 고려하여 현재 사용되고 있는 통신 객체들에 대해 정리하고 DLL로 제공하여 개발자로 하여금 통신의 내부적인 사항은 고려하지 않고 공개된 API를 통해 수신 데이터를 확인하거나 송신할 수 있도록 편의를 제공한다.   

## 기능
<hr/>
<img src="https://user-images.githubusercontent.com/65689549/85645923-fa309a80-b6d5-11ea-97d2-eed2cbabd9bc.png" width="650px" height="550px" title="px(픽셀) 크기 설정"></img><br/>

<img src="https://user-images.githubusercontent.com/65689549/84605654-f3d23f80-aed9-11ea-8ddf-67c1181e4783.png" width="350px" height="300px" title="px(픽셀) 크기 설정"></img><br/>

<img src="https://user-images.githubusercontent.com/65689549/84606049-3d705980-aedd-11ea-8e5d-1a40fef9a4f1.png" width="500px" height="350px" title="px(픽셀) 크기 설정"></img><br/>

<img src="https://user-images.githubusercontent.com/65689549/84606048-3c3f2c80-aedd-11ea-8b58-dd618472c144.png" width="600px" height="200px" title="px(픽셀) 크기 설정"></img><br/>

## 버전 관리 
<hr/>
V1. TCP(Server, Client) & Serial 각각 6채널씩 통신 가능하도록 구현</br>
V2. CMLib Runtime시 NIC가 꺼져있거나 IP Address가 다른 경우 죽는 문제 수정</br>
V3. Serial 데이터 송신 오류 발생 시, 해당 채널의 Thread 및 객체를 해제시켜 계속해서 사용 불가, Self-Recovery되도록 수정</br>
V4. UDP(Unicast, Multicast) 6채널 추가

## 개발 방향
<hr/>
1. 구현된 각종 통신 코드의 실 사용 중 발생되는 예외 상황 및 버그를 수정하고 CMLib에  반영</br>
2. 새로운 데이터 전송 방식 혹은 아직 구현되지 않은 통신 방식에 대해 스터디하고 CMLib에 반영
