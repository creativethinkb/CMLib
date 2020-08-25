# CMLib (Communication Middleware Library)
<hr/>
CMLib은 TCP, UDP, Serial 등의 통신 기능을 제공하고, Multi-Thread로 구성되어 동시에 여러 노드의 통신 채널과 통신이 가능한 Dynamic Link Library이다.

## 사용 목적
<hr/>
CMLib은 DLL 파일을 통해 API 함수를 제공하여 UI 개발자로 하여금 통신 관리와 송/수신 데이터 처리에 관한 부분을 고려할 필요 없이 구현할 수 있도록 설계하였다.
<br/>
<br/>
<img src="https://user-images.githubusercontent.com/65689549/84605654-f3d23f80-aed9-11ea-8ddf-67c1181e4783.png" width="250px" height="200px" title="px(픽셀) 크기 설정"></img>

## Sequence Diagram
<hr/>
<img src="https://user-images.githubusercontent.com/65689549/85645923-fa309a80-b6d5-11ea-97d2-eed2cbabd9bc.png" width="650px" height="550px" title="px(픽셀) 크기 설정"></img><br/>

## 구성
<hr/>
<img src="https://user-images.githubusercontent.com/65689549/86066899-66344980-baae-11ea-877c-42721f00f35c.png" width="450px" height="350px" title="px(픽셀) 크기 설정"></img><br/>

[Information Manager]</br>
개발하는 프로젝트마다 필요한 통신 타입과 통신의 개수, IP Address 및 통신 정보가 모두 다르기 때문에, 응용 프로그램에서 이러한 정보들을 가지고 있도록 개발을 하게 되면, 응용프로그램의 재사용 및 프로젝트 관리도 어려워지는 단점이 있다. 제안하는 통신 미들웨어(CMLib)는 통신 정보를 응용프로그램의 외부에 따로 기록한 특정 텍스트 파일을 참조하도록 하여 응용프로그램 개발 시 CMLib을 초기화하는 시점에 이 통신 정보 텍스트 파일을 읽어 현재 프로젝트에 사용할 통신의 수와 타입, IP Address 등의 통신 정보를 참조하여 동적으로 통신 객체를 생성하도록 하였다. 또한 이 관리자를 통해 외부로부터 접근되는 클라이언트의 통신 정보가 개발자가 텍스트 파일에 기록해둔 통신 정보와 일치하는 승인된 클라이언트인지 확인할 수 있도록 통신 정보를 로컬 변수에 저장하여 정보를 비교하는 역할을 한다.  </br>

[Communication Manager] </br>
CMLib의 통신 세션을 관리하며, Information Manager를 통해 생성된 Listener Thread에서 CMLib으로 접속하려는 클라이언트의 정보를 주기적으로 관찰한다. 접근이 발생한 경우 클라이언트의 통신 정보를 획득하고 Information Manager의 통신 정보 변수에 저장되어 있는 정보와 일치하는지 비교하고 승인된 클라이언트와 접속 세션을 맺는다. 세션이 생성되면 Data Manager로 하여금 Data Receive Thread와 Data Send Thread를 활성화시키고, 클라이언트로부터 수신되는 데이터를 처리할 준비를 시작하도록 하는 역할을 한다.  </br>

[Data Manager] </br>
Communication Manager를 통해 데이터를 수신할 준비가 완료되면, Data Manager에서는 Data Send Thread와 Data Receive Thread가 활성화된다. Data Receive Thread를 통해 수신된 데이터가 있는지 주기적으로 관찰을 시작하고 데이터 수신이 발생한 경우, Queue의 상태를 확인하고 데이터를 Queue에 입력한다. Data Send Thread는 Queue에 데이터가 입력되었는지 주기적으로 관찰하다가, 데이터가 입력되면 Queue의 상태를 확인하여 데이터를 출력한 뒤, 버퍼에 데이터를 넣고 버퍼의 내용을 목적지로 송신한다. </br>

[API Manager]  </br>
CMLib은 DLL 형태로 구현되어 있다. UI 개발자는 프로그램을 개발할 때 CMLib을 프로젝트에 참조하여 API Manager에서 제공하는 CMLib의 외부 노출 함수를 사용하도록 구성하였다. </br>

## 버전 관리 
<hr/>
V1. TCP(Server, Client) & Serial 각각 6채널씩 통신 구현</br>
V2. CMLib Runtime시 NIC가 꺼져있거나 IP Address가 다른 경우 죽는 문제 수정</br>
V3. Serial 데이터 송신 오류 발생 시, 해당 채널의 Thread 및 객체를 해제시켜 계속해서 사용 불가한 증상, Self-Recovery되도록 수정</br>
V4. UDP(Unicast, Multicast) 통신 방식 6채널 추가</br>
V5. UDP 추가된 부분 
    - 30Hz -> 100Hz Read[Hz별 전송테스트하여 100Hz Read결정]</br>
    (전송 시간 테스트 결과 100Hz=0.011초, 200Hz=0.016초, 500Hz=0.016초, 1000Hz=0.9초) </br>
    - MulticastLoopBack을 false로 설정하여 송신 시, 수신 Thread에 걸리지 않도록 설정</br>

## 개발 방향
<hr/>
1. 인터페이스 소프트웨어에 적용하여 실 사용 중 발생되는 예외 상황 및 버그를 수정하고 CMLib에  반영</br>
2. 새로운 데이터 전송 방식 혹은 아직 구현되지 않은 통신 방식에 대해 스터디하고 CMLib에 반영
