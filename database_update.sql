-- Supabase SQL Editor에서 이 쿼리를 실행하여 테이블 스킴을 업데이트하세요.

-- 1. 방 차단 유저 목록 및 최대 인원수 컬럼 추가
ALTER TABLE rooms 
ADD COLUMN IF NOT EXISTS banned_user_ids TEXT DEFAULT '[]',
ADD COLUMN IF NOT EXISTS max_participants INTEGER DEFAULT 5;

-- 2. (선택사항) participants 컬럼이 없다면 생성
-- ALTER TABLE rooms ADD COLUMN IF NOT EXISTS participants TEXT DEFAULT '[]';

-- 3. 실시간 변경 감지(Realtime) 활성화 확인
-- 이미 활성화되어 있다면 생략 가능합니다.
-- alter publication supabase_realtime add table rooms;
