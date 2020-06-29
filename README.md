# CMLib (Communication Middleware Library)
<hr/>
CMLib은 TCP, UDP, Serial 등의 통신 기능을 제공하고, Multi-Thread로 구성되어 동시에 여러 노드의 통신 채널과 고속의 통신이 가능한 Dynamic Link Library입니다. 

## 사용 목적
<hr/>
CMLib은 DLL 파일을 통해 API 함수를 제공하여 front-end 개발자로 하여금 통신 관리와 송/수신 데이터 처리에 관한 부분을 고려할 필요 없이 구현할 수 있도록 설계하였습니다.
<br/>
<br/>
<img src="https://user-images.githubusercontent.com/65689549/84605654-f3d23f80-aed9-11ea-8ddf-67c1181e4783.png" width="250px" height="200px" title="px(픽셀) 크기 설정"></img>

## Sequence Diagram
<hr/>
<img src="https://user-images.githubusercontent.com/65689549/85645923-fa309a80-b6d5-11ea-97d2-eed2cbabd9bc.png" width="650px" height="550px" title="px(픽셀) 크기 설정"></img><br/>

## 버전 관리 
<hr/>
V1. TCP(Server, Client) & Serial 각각 6채널씩 통신 구현</br>
V2. CMLib Runtime시 NIC가 꺼져있거나 IP Address가 다른 경우 죽는 문제 수정</br>
V3. Serial 데이터 송신 오류 발생 시, 해당 채널의 Thread 및 객체를 해제시켜 계속해서 사용 불가한 증상, Self-Recovery되도록 수정</br>
V4. UDP(Unicast, Multicast) 통신 방식 6채널 추가</br>
V5. 현재 설정된 30Hz 이상의 통신 데이터를 수신하는 경우가 있어 각 통신의 Data Read 주기를 증가하여 테스트 진행(버전 업데이트 예정)</br> 

## 개발 방향
<hr/>
1. 인터페이스 소프트웨어에 적용하여 실 사용 중 발생되는 예외 상황 및 버그를 수정하고 CMLib에  반영</br>
2. 새로운 데이터 전송 방식 혹은 아직 구현되지 않은 통신 방식에 대해 스터디하고 CMLib에 반영
