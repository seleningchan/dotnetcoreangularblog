<<<<<<< HEAD
# dotnetcoreangularblog
angular博客
=======
# Ancorazor

[![Codacy grade](https://img.shields.io/codacy/grade/00a15dd7811e42b7ae6aea01a966fee0.svg?logo=codacy&style=for-the-badge)](https://app.codacy.com/app/siegrainwong/ancorazor?utm_source=github.com&utm_medium=referral&utm_content=siegrainwong/ancorazor&utm_campaign=Badge_Grade_Dashboard)
[![Azure DevOps builds](https://img.shields.io/azure-devops/build/siegrainwong/75cdd93a-e41e-4158-ace3-88dab60c3343/6.svg?label=azure%20pipelines&logo=azure%20pipelines&style=for-the-badge)](https://dev.azure.com/siegrainwong/Ancorazor/_build/latest?definitionId=6&branchName=master)
[![LICENSE](https://img.shields.io/badge/license-Anti--996%20&%20MIT-blue.svg?style=for-the-badge)](https://github.com/996icu/996.ICU/blob/master/LICENSE)

[English Readme](https://github.com/siegrainwong/ancorazor/blob/master/README-EN.md)

---

Ancorazor ��һ������ .NET Core 2.2 �� Angular 7 �ļ��򲩿�ϵͳ��

[Demo](https://siegrain.wang)

_��Ŀ��Ȼ�ڿ����У����һ�û������̨������ǰ̨Ҳ�л����Ĺ����ܣ�������ǿ���õĽ׶Ρ�_

## ��ʾ

GIF 3M

![ancorazor gif demostration](https://s2.ax1x.com/2019/06/28/ZMxQs0.gif)

ת������ģ�����һ��˽ϴ�������`animate.css`д�ģ���Ϊ����`Angular animation`��̫���ã�2333��

## ������Ŀ

### ��������

ȷ�����Ļ����Ѿ�����Щ�����ˣ�

1. .NET Core 2.2 SDK
2. Nodejs 10+
3. SQL Server(�� docker-compose ���Բ������)

### ��������

1. `git clone https://github.com/siegrainwong/ancorazor.git`
2. �滻`ancorazor/Ancorazor.API/appsettings.Development.json`�е������ַ���(��ѡ��ȡ�����㱾�ص� SQL Server ���ã�һ�㲻��Ҫ�滻)
3. �� `cd path-to-ancorazor/Ancorazor.API` ����Ŀ¼��ִ�� `dotnet watch run`
4. �� `localhost:8088`, Ĭ���û������� admin/123456.

### docker-compose ����

`cd path-to-ancorazor/build`

#### windows

����`dev.ps1`����������`F:\Projects\ancorazor\`·���ַ����滻����ģ�Ȼ����������ű�

#### linux

���� `path-to-ancorazor/build/dev.sh`

docker-compose �Ὣ sql server��skywalking��nginx �� ancorazor һ��������

- Skywalking: `localhost:8080`, Ĭ���û������� is admin/admin.
- Ancorazor: `localhost:8088`, Ĭ���û������� is admin/123456.

## ����(CI/CD)

�һ���֮��дһƪ�̳������`Azure DevOps`�Ͻ��� CI/CD��������Ҳ���Բο� [azure-pipelines.yml](https://github.com/siegrainwong/ancorazor/blob/master/azure-pipelines.yml)��

## ��Ŀ�ṹ

TODO

## To-do

- [x] Comment
- [ ] Management page
- [ ] Search
- [ ] Categories & tags page
- [ ] Tests

��ο� [project](https://github.com/siegrainwong/ancorazor/projects/1).

## ��л

[ģ��: startbootstrap-clean-blog](https://github.com/BlackrockDigital/startbootstrap-clean-blog)

##

## Licence

Anti-996 & MIT

[![FOSSA Status](https://app.fossa.io/api/projects/git%2Bgithub.com%2Fsiegrainwong%2Fancorazor.svg?type=large)](https://app.fossa.io/projects/git%2Bgithub.com%2Fsiegrainwong%2Fancorazor?ref=badge_large)
>>>>>>> commit blog
